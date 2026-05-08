using System.Data;
using System.Text.Json;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public sealed class GettingTherePageContentRepository : IGettingTherePageContentRepository
{
    private const string GetContentProcedureName = "usp_GettingTherePageContent_Get";
    private const string UpsertContentProcedureName = "usp_GettingTherePageContent_Upsert";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _connectionString;

    public GettingTherePageContentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<GettingTherePageContent> GetAsync(CancellationToken cancellationToken = default)
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

    public async Task<GettingTherePageContent> UpsertAsync(
        GettingTherePageContent content,
        CancellationToken cancellationToken = default)
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
                $"La base de datos no tiene el procedimiento {UpsertContentProcedureName}. Ejecuta DB/DB-Ingenieria-Purrujas.sql para integrar la persistencia de Como llegar.",
                ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("No fue posible guardar el contenido de Como llegar.", ex);
        }
    }

    private static GettingTherePageContent MergeWithStoredContent(
        GettingTherePageContent defaultContent,
        string sectionTitle,
        string sectionTag,
        string storedJson)
    {
        GettingTherePageContent? storedContent = null;

        if (!string.IsNullOrWhiteSpace(storedJson))
        {
            try
            {
                storedContent = JsonSerializer.Deserialize<GettingTherePageContent>(storedJson, JsonOptions);
            }
            catch (JsonException)
            {
                storedContent = null;
            }
        }

        return new GettingTherePageContent
        {
            SectionTag = Coalesce(storedContent?.SectionTag, sectionTag, defaultContent.SectionTag),
            SectionTitle = Coalesce(storedContent?.SectionTitle, sectionTitle, defaultContent.SectionTitle),
            SectionSubtext = Coalesce(storedContent?.SectionSubtext, defaultContent.SectionSubtext),
            CoordinatesTitle = Coalesce(storedContent?.CoordinatesTitle, defaultContent.CoordinatesTitle),
            CoordinatesDescription = Coalesce(storedContent?.CoordinatesDescription, defaultContent.CoordinatesDescription),
            DirectionsItems = NormalizeListOrFallback(storedContent?.DirectionsItems, defaultContent.DirectionsItems),
            MapButtonLabel = Coalesce(storedContent?.MapButtonLabel, defaultContent.MapButtonLabel)
        };
    }

    private static GettingTherePageContent ValidateAndNormalize(GettingTherePageContent? content)
    {
        if (content is null)
        {
            throw new ArgumentException("El contenido de Como llegar es requerido.");
        }

        var directionsItems = NormalizeList(content.DirectionsItems);

        if (directionsItems.Count == 0)
        {
            throw new ArgumentException("La lista de indicaciones de Como llegar debe contener al menos un elemento.");
        }

        return new GettingTherePageContent
        {
            SectionTag = RequireValue(content.SectionTag, "La etiqueta de la seccion es requerida."),
            SectionTitle = RequireValue(content.SectionTitle, "El titulo de la seccion es requerido."),
            SectionSubtext = RequireValue(content.SectionSubtext, "El texto introductorio es requerido."),
            CoordinatesTitle = RequireValue(content.CoordinatesTitle, "El titulo de coordenadas es requerido."),
            CoordinatesDescription = RequireValue(content.CoordinatesDescription, "La descripcion de coordenadas es requerida."),
            DirectionsItems = directionsItems,
            MapButtonLabel = RequireValue(content.MapButtonLabel, "La etiqueta del boton del mapa es requerida.")
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

    private static GettingTherePageContent CreateDefaultContent()
    {
        return new GettingTherePageContent
        {
            SectionTag = "Visítanos",
            SectionTitle = "¿Cómo llegar?",
            SectionSubtext = "A 45 minutos de San José, en las faldas del Volcán Turrialba.",
            CoordinatesTitle = "Coordenadas",
            CoordinatesDescription = "9.975878207007307° N,83.770258333651° W · Las Purrujas, Cartago.",
            DirectionsItems =
            [
                "Ruta 32 hasta Turrialba, luego desvío a La Pastora.",
                "Transporte privado disponible desde el aeropuerto SJO.",
                "Parqueo gratuito y seguro dentro de la propiedad."
            ],
            MapButtonLabel = "Abrir en Google Maps"
        };
    }
}
