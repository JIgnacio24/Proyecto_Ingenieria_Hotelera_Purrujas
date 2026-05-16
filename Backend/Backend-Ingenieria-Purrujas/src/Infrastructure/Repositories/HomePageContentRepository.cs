using System.Data;
using System.Text.Json;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public sealed class HomePageContentRepository : IHomePageContentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _connectionString;

    public HomePageContentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<HomePageContent> GetAsync(CancellationToken cancellationToken = default)
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
            await EnsureSchemaAsync(connection, cancellationToken);
            await using var command = new SqlCommand(
                """
                SELECT TOP 1 ISNULL(ContentJson, '{}') AS ContentJson
                FROM HomePageContent
                ORDER BY CreatedAt DESC
                """,
                connection);

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

    public async Task<HomePageContent> UpsertAsync(HomePageContent content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("No se configuró la conexión a la base de datos.");
        }

        var normalizedContent = ValidateAndNormalize(content);
        var serializedContent = JsonSerializer.Serialize(normalizedContent, JsonOptions);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await EnsureSchemaAsync(connection, cancellationToken);
            await using var command = new SqlCommand(
                """
                IF EXISTS (SELECT 1 FROM HomePageContent)
                BEGIN
                    UPDATE HomePageContent
                    SET ContentJson = @ContentJson,
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = (SELECT TOP 1 Id FROM HomePageContent ORDER BY CreatedAt DESC);
                END
                ELSE
                BEGIN
                    INSERT INTO HomePageContent (ContentJson, CreatedAt, UpdatedAt)
                    VALUES (@ContentJson, GETUTCDATE(), GETUTCDATE());
                END
                """,
                connection);

            var contentParameter = command.Parameters.Add("@ContentJson", SqlDbType.NVarChar, -1);
            contentParameter.Value = serializedContent;

            await command.ExecuteNonQueryAsync(cancellationToken);
            return normalizedContent;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("No fue posible guardar el contenido del inicio.", ex);
        }
    }

    private static async Task EnsureSchemaAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // El contenido de Home se mantiene autocontenido para no depender de una migracion manual durante desarrollo.
        await using var command = new SqlCommand(
            """
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HomePageContent')
            BEGIN
                CREATE TABLE HomePageContent (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    ContentJson NVARCHAR(MAX) NOT NULL DEFAULT '{}',
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );
            END;
            """,
            connection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static HomePageContent MergeWithStoredContent(HomePageContent defaultContent, string storedJson)
    {
        // Mezcla el JSON almacenado con defaults para tolerar registros incompletos.
        HomePageContent? storedContent = null;

        if (!string.IsNullOrWhiteSpace(storedJson))
        {
            try
            {
                storedContent = JsonSerializer.Deserialize<HomePageContent>(storedJson, JsonOptions);
            }
            catch (JsonException)
            {
                storedContent = null;
            }
        }

        return new HomePageContent
        {
            HeroEyebrow = Coalesce(storedContent?.HeroEyebrow, defaultContent.HeroEyebrow),
            HeroTitle = Coalesce(storedContent?.HeroTitle, defaultContent.HeroTitle),
            HeroSubtitle = Coalesce(storedContent?.HeroSubtitle, defaultContent.HeroSubtitle)
        };
    }

    private static HomePageContent ValidateAndNormalize(HomePageContent? content)
    {
        if (content is null)
        {
            throw new ArgumentException("El contenido del inicio es requerido.");
        }

        return new HomePageContent
        {
            HeroEyebrow = RequireValue(content.HeroEyebrow, "La etiqueta del hero es requerida."),
            HeroTitle = RequireValue(content.HeroTitle, "El título del hero es requerido."),
            HeroSubtitle = RequireValue(content.HeroSubtitle, "El subtítulo del hero es requerido.")
        };
    }

    private static string RequireValue(string? value, string errorMessage)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(normalized) ? throw new ArgumentException(errorMessage) : normalized;
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

    private static HomePageContent CreateDefaultContent()
    {
        // Contenido base equivalente al hero historico del frontend cliente.
        return new HomePageContent
        {
            HeroEyebrow = "Bienvenido",
            HeroTitle = "Hotel Las Purrujas",
            HeroSubtitle = "Donde la naturaleza te abraza y Costa Rica te enamora."
        };
    }
}
