using System.Data;
using System.Text.Json;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public sealed class AboutUsPageContentRepository : IAboutUsPageContentRepository
{
    private const string GetContentProcedureName = "usp_AboutUsPageContent_Get";
    private const string UpsertContentProcedureName = "usp_AboutUsPageContent_Upsert";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _connectionString;

    public AboutUsPageContentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<AboutUsPageContent> GetAsync(CancellationToken cancellationToken = default)
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

            var storedJson = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            return MergeWithStoredContent(defaultContent, storedJson);
        }
        catch
        {
            return defaultContent;
        }
    }

    public async Task<AboutUsPageContent> UpsertAsync(
        AboutUsPageContent content,
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

            var contentParameter = command.Parameters.Add("@ContentJson", SqlDbType.NVarChar, -1);
            contentParameter.Value = serializedContent;

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
                $"La base de datos no tiene el procedimiento {UpsertContentProcedureName}. Ejecuta DB/DB-Ingenieria-Purrujas.sql o DB/Patches/2026-05-06-about-us-content.sql.",
                ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("No fue posible guardar el contenido de Sobre Nosotros.", ex);
        }
    }

    private static AboutUsPageContent MergeWithStoredContent(
        AboutUsPageContent defaultContent,
        string storedJson)
    {
        // El JSON puede venir de versiones anteriores del sitio.
        // Por eso se mezcla con defaults para no perder secciones si falta una propiedad.
        AboutUsPageContent? storedContent = null;

        if (!string.IsNullOrWhiteSpace(storedJson))
        {
            try
            {
                storedContent = JsonSerializer.Deserialize<AboutUsPageContent>(storedJson, JsonOptions);
            }
            catch (JsonException)
            {
                storedContent = null;
            }
        }

        return new AboutUsPageContent
        {
            HistoryTag = Coalesce(storedContent?.HistoryTag, defaultContent.HistoryTag),
            HistoryTitle = Coalesce(storedContent?.HistoryTitle, defaultContent.HistoryTitle),
            HistoryDescription = Coalesce(storedContent?.HistoryDescription, defaultContent.HistoryDescription),
            HistoryTimelineStartYear = Coalesce(storedContent?.HistoryTimelineStartYear, defaultContent.HistoryTimelineStartYear),
            HistoryMilestones = NormalizeListOrFallback(storedContent?.HistoryMilestones, defaultContent.HistoryMilestones),
            HistoryTimelineEndLabel = Coalesce(storedContent?.HistoryTimelineEndLabel, defaultContent.HistoryTimelineEndLabel),
            TeamTag = Coalesce(storedContent?.TeamTag, defaultContent.TeamTag),
            TeamTitle = Coalesce(storedContent?.TeamTitle, defaultContent.TeamTitle),
            CollaboratorsCount = PositiveOrFallback(storedContent?.CollaboratorsCount, defaultContent.CollaboratorsCount),
            LocalTalentPercentage = PositiveOrFallback(storedContent?.LocalTalentPercentage, defaultContent.LocalTalentPercentage),
            ExperienceYears = PositiveOrFallback(storedContent?.ExperienceYears, defaultContent.ExperienceYears),
            DirectorName = Coalesce(storedContent?.DirectorName, defaultContent.DirectorName),
            DirectorTitle = Coalesce(storedContent?.DirectorTitle, defaultContent.DirectorTitle),
            DirectorBiography = Coalesce(storedContent?.DirectorBiography, defaultContent.DirectorBiography),
            PhilosophyTitle = Coalesce(storedContent?.PhilosophyTitle, defaultContent.PhilosophyTitle),
            PhilosophyDescription = Coalesce(storedContent?.PhilosophyDescription, defaultContent.PhilosophyDescription),
            PhilosophyQuote = Coalesce(storedContent?.PhilosophyQuote, defaultContent.PhilosophyQuote),
            MvvTag = Coalesce(storedContent?.MvvTag, defaultContent.MvvTag),
            MvvTitle = Coalesce(storedContent?.MvvTitle, defaultContent.MvvTitle),
            MissionTitle = Coalesce(storedContent?.MissionTitle, defaultContent.MissionTitle),
            Mission = Coalesce(storedContent?.Mission, defaultContent.Mission),
            VisionTitle = Coalesce(storedContent?.VisionTitle, defaultContent.VisionTitle),
            Vision = Coalesce(storedContent?.Vision, defaultContent.Vision),
            ValuesTitle = Coalesce(storedContent?.ValuesTitle, defaultContent.ValuesTitle),
            Values = NormalizeListOrFallback(storedContent?.Values, defaultContent.Values),
            GalleryTag = Coalesce(storedContent?.GalleryTag, defaultContent.GalleryTag),
            GalleryTitle = Coalesce(storedContent?.GalleryTitle, defaultContent.GalleryTitle),
            GallerySubtext = Coalesce(storedContent?.GallerySubtext, defaultContent.GallerySubtext)
        };
    }

    private static AboutUsPageContent ValidateAndNormalize(AboutUsPageContent? content)
    {
        // Normaliza la entrada del editor antes de persistirla.
        // Evita guardar listas vacias o textos requeridos solo con espacios.
        if (content is null)
        {
            throw new ArgumentException("El contenido de Sobre Nosotros es requerido.");
        }

        var values = NormalizeList(content.Values);
        var historyMilestones = NormalizeList(content.HistoryMilestones);
        if (historyMilestones.Count == 0)
        {
            throw new ArgumentException("Debe existir al menos un hito en la tarjeta de historia.");
        }

        if (values.Count == 0)
        {
            throw new ArgumentException("Debe existir al menos un valor.");
        }

        return new AboutUsPageContent
        {
            HistoryTag = RequireValue(content.HistoryTag, "La etiqueta de historia es requerida."),
            HistoryTitle = RequireValue(content.HistoryTitle, "El titulo de historia es requerido."),
            HistoryDescription = RequireValue(content.HistoryDescription, "La descripcion de historia es requerida."),
            HistoryTimelineStartYear = RequireValue(content.HistoryTimelineStartYear, "El ano inicial de la tarjeta de historia es requerido."),
            HistoryMilestones = historyMilestones,
            HistoryTimelineEndLabel = RequireValue(content.HistoryTimelineEndLabel, "La etiqueta final de la tarjeta de historia es requerida."),
            TeamTag = RequireValue(content.TeamTag, "La etiqueta del equipo es requerida."),
            TeamTitle = RequireValue(content.TeamTitle, "El titulo del equipo es requerido."),
            CollaboratorsCount = RequirePositive(content.CollaboratorsCount, "El numero de colaboradores debe ser mayor a 0."),
            LocalTalentPercentage = RequirePositive(content.LocalTalentPercentage, "El porcentaje de talento local debe ser mayor a 0."),
            ExperienceYears = RequirePositive(content.ExperienceYears, "Los anos de experiencia deben ser mayor a 0."),
            DirectorName = RequireValue(content.DirectorName, "El nombre del director es requerido."),
            DirectorTitle = RequireValue(content.DirectorTitle, "El cargo del director es requerido."),
            DirectorBiography = RequireValue(content.DirectorBiography, "La biografia del director es requerida."),
            PhilosophyTitle = RequireValue(content.PhilosophyTitle, "El titulo de filosofia es requerido."),
            PhilosophyDescription = RequireValue(content.PhilosophyDescription, "La descripcion de filosofia es requerida."),
            PhilosophyQuote = RequireValue(content.PhilosophyQuote, "La cita de filosofia es requerida."),
            MvvTag = RequireValue(content.MvvTag, "La etiqueta de mision, vision y valores es requerida."),
            MvvTitle = RequireValue(content.MvvTitle, "El titulo de mision, vision y valores es requerido."),
            MissionTitle = RequireValue(content.MissionTitle, "El titulo de mision es requerido."),
            Mission = RequireValue(content.Mission, "La mision es requerida."),
            VisionTitle = RequireValue(content.VisionTitle, "El titulo de vision es requerido."),
            Vision = RequireValue(content.Vision, "La vision es requerida."),
            ValuesTitle = RequireValue(content.ValuesTitle, "El titulo de valores es requerido."),
            Values = values,
            GalleryTag = RequireValue(content.GalleryTag, "La etiqueta de galeria es requerida."),
            GalleryTitle = RequireValue(content.GalleryTitle, "El titulo de galeria es requerido."),
            GallerySubtext = RequireValue(content.GallerySubtext, "El subtexto de galeria es requerido.")
        };
    }

    private static int PositiveOrFallback(int? value, int fallback)
    {
        return value > 0 ? value.Value : fallback;
    }

    private static int RequirePositive(int value, string errorMessage)
    {
        return value > 0 ? value : throw new ArgumentException(errorMessage);
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

    private static AboutUsPageContent CreateDefaultContent()
    {
        // Contenido base usado cuando la tabla aun no tiene registro o el JSON almacenado es invalido.
        return new AboutUsPageContent
        {
            HistoryTag = "Desde 2005",
            HistoryTitle = "Nuestra Historia",
            HistoryDescription = "Hotel Las Purrujas nacio en el ano 2005 en el corazon de los Andes costarricenses, especificamente en las faldas del Volcan Turrialba, en la provincia de Cartago. Su nombre rinde homenaje a las purrujas, pequenas aves endemicas de la region que simbolizan la vida silvestre y la conexion profunda con la naturaleza.\n\nFundado por la familia Vargas Montoya, el hotel comenzo como una pequena posada de cuatro habitaciones con el sueno de ofrecer una experiencia autentica del campo costarricense. Con los anos y gracias al turismo ecologico, se convirtio en un referente del ecoturismo en la zona central de Costa Rica.",
            HistoryTimelineStartYear = "2005",
            HistoryMilestones =
            [
                "Fundacion con 4 habitaciones",
                "Expansion del restaurante La Ceiba",
                "18 habitaciones tematicas",
                "Referente de ecoturismo en Cartago"
            ],
            HistoryTimelineEndLabel = "Hoy",
            TeamTag = "Nuestra gente",
            TeamTitle = "Equipo & Filosofia",
            CollaboratorsCount = 30,
            LocalTalentPercentage = 90,
            ExperienceYears = 20,
            DirectorName = "Andrea Vargas",
            DirectorTitle = "Directora General",
            DirectorBiography = "Hija de los fundadores y graduada en Administracion Hotelera de la Universidad de Costa Rica, Andrea lidera el hotel con una vision moderna sin perder la esencia familiar que lo caracteriza. Bajo su direccion, Las Purrujas ha crecido como referente de ecoturismo responsable en la region.",
            PhilosophyTitle = "Nuestra Filosofia",
            PhilosophyDescription = "En Las Purrujas no solo ofrecemos una cama y un desayuno; ofrecemos una experiencia de vida. Cada detalle, desde la decoracion artesanal hasta el menu del restaurante, esta pensado para que el huesped se lleve consigo un pedazo autentico de Costa Rica. Creemos que el turismo puede y debe ser un motor de desarrollo local, por eso reinvertimos parte de nuestros ingresos en programas educativos y ambientales para la comunidad.",
            PhilosophyQuote = "Donde la naturaleza te abraza y Costa Rica te enamora.",
            MvvTag = "Quienes somos",
            MvvTitle = "Mision, Vision & Valores",
            MissionTitle = "Mision",
            Mission = "Brindar a nuestros huespedes una experiencia de hospedaje autentica, calida y sostenible, conectandolos con la riqueza natural y cultural de Costa Rica, a traves de un servicio personalizado y comprometido con el bienestar de la comunidad local y el medio ambiente.",
            VisionTitle = "Vision",
            Vision = "Ser reconocidos como el principal destino de ecoturismo en la region de Cartago para el ano 2030, liderando un modelo de turismo responsable que inspire a otras empresas hoteleras a adoptar practicas sostenibles.",
            ValuesTitle = "Valores",
            Values =
            [
                "Sostenibilidad",
                "Calidez humana",
                "Compromiso comunitario",
                "Respeto por la naturaleza",
                "Excelencia en el servicio"
            ],
            GalleryTag = "Inspirate",
            GalleryTitle = "Galeria",
            GallerySubtext = "Descubre las instalaciones del hotel y los maravillosos lugares que te rodean para planificar tu itinerario perfecto."
        };
    }
}
