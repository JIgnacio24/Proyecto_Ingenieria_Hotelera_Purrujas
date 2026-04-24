using System.Text.Json;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public sealed class FacilitiesPageContentRepository : IFacilitiesPageContentRepository
{
    private const string GetContentProcedureName = "usp_FacilitiesPageContent_Get";
    private const string UpsertContentProcedureName = "usp_FacilitiesPageContent_Upsert";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _connectionString;

    public FacilitiesPageContentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<FacilitiesPageContent> GetAsync(CancellationToken cancellationToken = default)
    {
        var defaultContent = CreateDefaultContent();

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return defaultContent;
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(GetContentProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return defaultContent;
            }

            var sectionTitle = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var sectionTag = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var storedJson = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            return MergeWithStoredContent(defaultContent, sectionTitle, sectionTag, storedJson);
        }
        catch
        {
            return defaultContent;
        }
    }

    public async Task<FacilitiesPageContent> UpsertAsync(FacilitiesPageContent content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("No se configuro la conexion a la base de datos.");
        }

        var normalizedContent = ValidateAndNormalize(content);
        var serializedContent = JsonSerializer.Serialize(normalizedContent, JsonOptions);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(UpsertContentProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SectionTitle", normalizedContent.SectionTitle);
            command.Parameters.AddWithValue("@SectionTag", normalizedContent.SectionTag);
            var descriptionParameter = command.Parameters.Add("@DescriptionJson", SqlDbType.NVarChar, -1);
            descriptionParameter.Value = serializedContent;

            await command.ExecuteNonQueryAsync(cancellationToken);
            return normalizedContent;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (SqlException ex) when (ex.Message.Contains(UpsertContentProcedureName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"La base de datos no tiene el procedimiento {UpsertContentProcedureName}. Ejecuta DB/DB-Ingenieria-Purrujas.sql para integrar la persistencia de Facilidades.",
                ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("No fue posible guardar el contenido de Facilidades.", ex);
        }
    }

    private static FacilitiesPageContent MergeWithStoredContent(
        FacilitiesPageContent defaultContent,
        string sectionTitle,
        string sectionTag,
        string storedJson)
    {
        FacilitiesPageContent? storedContent = null;

        if (!string.IsNullOrWhiteSpace(storedJson))
        {
            try
            {
                storedContent = JsonSerializer.Deserialize<FacilitiesPageContent>(storedJson, JsonOptions);
            }
            catch (JsonException)
            {
                storedContent = null;
            }
        }

        var primaryItems = NormalizeListOrFallback(storedContent?.PrimaryListItems, defaultContent.PrimaryListItems);
        var secondaryItems = NormalizeListOrFallback(storedContent?.SecondaryListItems, defaultContent.SecondaryListItems);
        var serviceCards = NormalizeServiceCardsOrFallback(storedContent?.ServiceCards, defaultContent.ServiceCards);

        return new FacilitiesPageContent
        {
            SectionTag = Coalesce(storedContent?.SectionTag, sectionTag, defaultContent.SectionTag),
            SectionTitle = Coalesce(storedContent?.SectionTitle, sectionTitle, defaultContent.SectionTitle),
            HighlightTitle = Coalesce(storedContent?.HighlightTitle, defaultContent.HighlightTitle),
            HighlightDescription = Coalesce(storedContent?.HighlightDescription, defaultContent.HighlightDescription),
            PrimaryListTitle = Coalesce(storedContent?.PrimaryListTitle, defaultContent.PrimaryListTitle),
            PrimaryListItems = primaryItems,
            SecondaryListTitle = Coalesce(storedContent?.SecondaryListTitle, defaultContent.SecondaryListTitle),
            SecondaryListItems = secondaryItems,
            ServiceCards = serviceCards
        };
    }

    private static FacilitiesPageContent ValidateAndNormalize(FacilitiesPageContent? content)
    {
        if (content is null)
        {
            throw new ArgumentException("El contenido de Facilidades es requerido.");
        }

        var primaryItems = NormalizeList(content.PrimaryListItems);
        var secondaryItems = NormalizeList(content.SecondaryListItems);
        var serviceCards = NormalizeServiceCards(content.ServiceCards);

        if (primaryItems.Count == 0)
        {
            throw new ArgumentException("La lista principal de Facilidades debe contener al menos un elemento.");
        }

        if (secondaryItems.Count == 0)
        {
            throw new ArgumentException("La lista secundaria de Facilidades debe contener al menos un elemento.");
        }

        if (serviceCards.Count == 0)
        {
            throw new ArgumentException("Debe existir al menos una tarjeta de servicio en Facilidades.");
        }

        return new FacilitiesPageContent
        {
            SectionTag = RequireValue(content.SectionTag, "La etiqueta de la seccion es requerida."),
            SectionTitle = RequireValue(content.SectionTitle, "El titulo de la seccion es requerido."),
            HighlightTitle = RequireValue(content.HighlightTitle, "El titulo destacado es requerido."),
            HighlightDescription = RequireValue(content.HighlightDescription, "La descripcion destacada es requerida."),
            PrimaryListTitle = RequireValue(content.PrimaryListTitle, "El titulo de la lista principal es requerido."),
            PrimaryListItems = primaryItems,
            SecondaryListTitle = RequireValue(content.SecondaryListTitle, "El titulo de la lista secundaria es requerido."),
            SecondaryListItems = secondaryItems,
            ServiceCards = serviceCards
        };
    }

    private static string RequireValue(string? value, string errorMessage)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException(errorMessage);
        }

        return normalized;
    }

    private static string Coalesce(params string?[] values)
    {
        foreach (var value in values)
        {
            var normalized = value?.Trim();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return string.Empty;
    }

    private static List<string> NormalizeList(IEnumerable<string>? items)
    {
        return (items ?? [])
            .Select(item => item?.Trim() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }

    private static List<string> NormalizeListOrFallback(IEnumerable<string>? items, IReadOnlyList<string> fallback)
    {
        var normalizedItems = NormalizeList(items);
        return normalizedItems.Count > 0 ? normalizedItems : [.. fallback];
    }

    private static List<FacilitiesServiceText> NormalizeServiceCards(IEnumerable<FacilitiesServiceText>? cards)
    {
        return (cards ?? [])
            .Select(card => new FacilitiesServiceText
            {
                Title = card?.Title?.Trim() ?? string.Empty,
                Description = card?.Description?.Trim() ?? string.Empty
            })
            .Where(card => !string.IsNullOrWhiteSpace(card.Title) && !string.IsNullOrWhiteSpace(card.Description))
            .ToList();
    }

    private static List<FacilitiesServiceText> NormalizeServiceCardsOrFallback(
        IReadOnlyList<FacilitiesServiceText>? storedCards,
        IReadOnlyList<FacilitiesServiceText> fallbackCards)
    {
        var normalizedStoredCards = NormalizeServiceCards(storedCards);
        if (normalizedStoredCards.Count == 0)
        {
            return CloneServiceCards(fallbackCards);
        }

        var totalCards = Math.Max(normalizedStoredCards.Count, fallbackCards.Count);
        var mergedCards = new List<FacilitiesServiceText>(totalCards);

        for (var index = 0; index < totalCards; index++)
        {
            var fallbackCard = index < fallbackCards.Count ? fallbackCards[index] : new FacilitiesServiceText();
            var storedCard = index < normalizedStoredCards.Count ? normalizedStoredCards[index] : null;

            mergedCards.Add(new FacilitiesServiceText
            {
                Title = Coalesce(storedCard?.Title, fallbackCard.Title),
                Description = Coalesce(storedCard?.Description, fallbackCard.Description)
            });
        }

        return mergedCards;
    }

    private static List<FacilitiesServiceText> CloneServiceCards(IEnumerable<FacilitiesServiceText> cards)
    {
        return cards
            .Select(card => new FacilitiesServiceText
            {
                Title = card.Title,
                Description = card.Description
            })
            .ToList();
    }

    private static FacilitiesPageContent CreateDefaultContent()
    {
        return new FacilitiesPageContent
        {
            SectionTag = "Lo que nos distingue",
            SectionTitle = "Caracteristicas Principales",
            HighlightTitle = "Ubicacion Privilegiada",
            HighlightDescription = "Situado a solo 45 minutos de San Jose, en las verdes montanas de Cartago, el hotel ofrece vistas panoramicas al Volcan Turrialba y esta rodeado de bosques nubosos y rios cristalinos. Una combinacion unica de accesibilidad y tranquilidad absoluta.",
            PrimaryListTitle = "Instalaciones",
            PrimaryListItems =
            [
                "18 habitaciones tematicas",
                "Restaurante \"La Ceiba\"",
                "Piscina natural de manantial",
                "Senderos ecologicos (5 km)",
                "Salon de eventos",
                "Spa con plantas locales"
            ],
            SecondaryListTitle = "Servicios Destacados",
            SecondaryListItems =
            [
                "Tours al Volcan Turrialba e Irazu",
                "Birdwatching con guias certificados",
                "Talleres de gastronomia tipica",
                "Transporte desde San Jose",
                "Wi-Fi de alta velocidad",
                "Atencion personalizada 24/7"
            ],
            ServiceCards =
            [
                new FacilitiesServiceText
                {
                    Title = "18 habitaciones tematicas",
                    Description = "Ambientes con personalidad propia, balcones al bosque nuboso y textiles artesanales inspirados en Cartago."
                },
                new FacilitiesServiceText
                {
                    Title = "Restaurante \"La Ceiba\"",
                    Description = "Cocina de finca a la mesa, cafe chorreado y menus de temporada que celebran los sabores locales."
                },
                new FacilitiesServiceText
                {
                    Title = "Piscina natural de manantial",
                    Description = "Agua cristalina, temperatura agradable y vistas verdes para recargar energia de forma natural."
                },
                new FacilitiesServiceText
                {
                    Title = "Senderos ecologicos (5 km)",
                    Description = "Rutas senalizadas entre bosque nuboso, ideales para caminatas al amanecer y observacion de flora."
                },
                new FacilitiesServiceText
                {
                    Title = "Salon de eventos",
                    Description = "Espacio versatil con luz natural, perfecto para retiros corporativos, bodas boutique y talleres."
                },
                new FacilitiesServiceText
                {
                    Title = "Spa con plantas locales",
                    Description = "Tratamientos herbales, masajes relajantes y aromaterapia con esencias del bosque costarricense."
                },
                new FacilitiesServiceText
                {
                    Title = "Tours al Volcan Turrialba e Irazu",
                    Description = "Excursiones guiadas para explorar dos volcanes iconicos con logistica y transporte incluidos."
                },
                new FacilitiesServiceText
                {
                    Title = "Birdwatching con guias certificados",
                    Description = "Avistamiento de purrujas y mas de 120 especies con especialistas locales y equipo optico."
                },
                new FacilitiesServiceText
                {
                    Title = "Talleres de gastronomia tipica",
                    Description = "Aprende a preparar tortillas palmeadas, gallo pinto y salsas caseras con cocineras de la zona."
                },
                new FacilitiesServiceText
                {
                    Title = "Transporte desde San Jose",
                    Description = "Traslados seguros puerta a puerta para que llegues sin preocupaciones desde el aeropuerto o la ciudad."
                },
                new FacilitiesServiceText
                {
                    Title = "Wi-Fi de alta velocidad",
                    Description = "Conectividad confiable en habitaciones y areas comunes para trabajar o compartir tu experiencia."
                },
                new FacilitiesServiceText
                {
                    Title = "Atencion personalizada 24/7",
                    Description = "Equipo disponible todo el dia para ayudarte con reservas, recomendaciones y soporte durante tu estadia."
                }
            ]
        };
    }

}
