using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class RoomTypeRepository : IRoomTypeRepository
{
    private readonly string _connectionString;
    private readonly ILogger<RoomTypeRepository> _logger;
    private static readonly Dictionary<string, RoomType> FallbackRoomTypes = new()
    {
        { "doble", new RoomType { RoomTypeId = 1, Name = "Habitación Doble", BasePrice = 95,  Capacity = 2 } },
        { "suite", new RoomType { RoomTypeId = 2, Name = "Suite Volcán",     BasePrice = 135, Capacity = 3 } },
        { "villa", new RoomType { RoomTypeId = 3, Name = "Villa Familiar",   BasePrice = 180, Capacity = 6 } }
    };

    public RoomTypeRepository(IConfiguration configuration, ILogger<RoomTypeRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RoomType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return FallbackRoomTypes.Values.ToList();

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = """
                SELECT RoomTypeId, Name, BasePrice, IsActive, Description, Capacity
                FROM RoomType
                WHERE IsActive = 1
                  AND LEN(LTRIM(RTRIM(Name))) > 0
                  AND BasePrice > 0
                ORDER BY Name ASC;
            """;

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var roomTypes = new List<RoomType>();

            while (await reader.ReadAsync(cancellationToken))
            {
                roomTypes.Add(MapRoomType(reader));
            }

            var result = DeduplicateRoomTypes(roomTypes);
            return result.Count > 0 ? result : FallbackRoomTypes.Values.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener los tipos de habitación; se usan valores de respaldo.");
            return FallbackRoomTypes.Values.ToList();
        }
    }

    public async Task<RoomType?> GetByIdAsync(int roomTypeId, CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            SELECT RoomTypeId, Name, BasePrice, IsActive, Description, Capacity
            FROM RoomType
            WHERE RoomTypeId = @RoomTypeId
              AND IsActive = 1
              AND LEN(LTRIM(RTRIM(Name))) > 0
              AND BasePrice > 0;
        """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@RoomTypeId", SqlDbType.Int).Value = roomTypeId;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRoomType(reader) : null;
    }

    public async Task<RoomType?> GetByKeyAsync(string roomKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return TryGetFallback(roomKey);
        }

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = """
                SELECT TOP 1 RoomTypeId, Name, BasePrice, IsActive, Description, Capacity
                FROM RoomType
                WHERE IsActive = 1
                  AND (
                    LTRIM(RTRIM(Name)) COLLATE Latin1_General_100_CI_AI = LTRIM(RTRIM(@nameKey)) COLLATE Latin1_General_100_CI_AI
                    OR LTRIM(RTRIM(Name)) COLLATE Latin1_General_100_CI_AI LIKE '%' + LTRIM(RTRIM(@nameKey)) + '%'
                  )
                ORDER BY
                    CASE WHEN Name LIKE N'%[ÁÉÍÓÚáéíóúÑñ]%' THEN 0 ELSE 1 END,
                    RoomTypeId;
            """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nameKey", roomKey);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapRoomType(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar el tipo de habitación '{RoomKey}'; se usa valor de respaldo.", roomKey);
        }

        return TryGetFallback(roomKey);
    }

    public async Task<RoomType> CreateAsync(RoomType roomType, CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(roomType.Name);
        ValidatePrice(roomType.BasePrice);

        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        await EnsureNameIsAvailableAsync(conn, normalizedName, null, cancellationToken);

        const string sql = """
            INSERT INTO RoomType (Name, BasePrice, IsActive, Description, Capacity)
            OUTPUT INSERTED.RoomTypeId, INSERTED.Name, INSERTED.BasePrice, INSERTED.IsActive, INSERTED.Description, INSERTED.Capacity
            VALUES (@Name, @BasePrice, 1, @Description, @Capacity);
        """;

        await using var cmd = new SqlCommand(sql, conn);
        AddRoomTypeParameters(cmd, normalizedName, roomType.BasePrice);
        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = (object?)roomType.Description ?? DBNull.Value;
        cmd.Parameters.Add("@Capacity", SqlDbType.Int).Value = roomType.Capacity;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("No fue posible crear el tipo de habitación.");
        }

        return MapRoomType(reader);
    }

    public async Task<RoomType?> UpdateAsync(RoomType roomType, CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(roomType.Name);
        ValidatePrice(roomType.BasePrice);

        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        await EnsureNameIsAvailableAsync(conn, normalizedName, roomType.RoomTypeId, cancellationToken);

        const string sql = """
            UPDATE RoomType
            SET Name = @Name,
                BasePrice = @BasePrice,
                Description = @Description,
                Capacity = @Capacity
            OUTPUT INSERTED.RoomTypeId, INSERTED.Name, INSERTED.BasePrice, INSERTED.IsActive, INSERTED.Description, INSERTED.Capacity
            WHERE RoomTypeId = @RoomTypeId
              AND IsActive = 1;
        """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@RoomTypeId", SqlDbType.Int).Value = roomType.RoomTypeId;
        AddRoomTypeParameters(cmd, normalizedName, roomType.BasePrice);
        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = (object?)roomType.Description ?? DBNull.Value;
        cmd.Parameters.Add("@Capacity", SqlDbType.Int).Value = roomType.Capacity;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRoomType(reader) : null;
    }

    public async Task<bool> DeleteAsync(int roomTypeId, CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE RoomType
            SET IsActive = 0
            WHERE RoomTypeId = @RoomTypeId
              AND IsActive = 1;
        """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@RoomTypeId", SqlDbType.Int).Value = roomTypeId;

        var affectedRows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0;
    }

    private static RoomType? TryGetFallback(string roomKey)
    {
        return FallbackRoomTypes.TryGetValue(roomKey.ToLowerInvariant(), out var roomType)
            ? roomType
            : null;
    }

    private SqlConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("No se configuró la cadena de conexión de SQL Server.");
        }

        return new SqlConnection(_connectionString);
    }

    private static RoomType MapRoomType(SqlDataReader reader)
    {
        return new RoomType
        {
            RoomTypeId  = reader.GetInt32(0),
            Name        = reader.GetString(1),
            BasePrice   = reader.GetDecimal(2),
            IsActive    = reader.GetBoolean(3),
            Description = reader.IsDBNull(4) ? null : reader.GetString(4),
            Capacity    = reader.GetInt32(5)
        };
    }

    private static string NormalizeName(string name)
    {
        var normalizedName = name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("El nombre del tipo de habitación es obligatorio.");
        }

        if (normalizedName.Length > 255)
        {
            throw new ArgumentException("El nombre del tipo de habitación no puede superar 255 caracteres.");
        }

        return normalizedName;
    }

    private static void ValidatePrice(decimal basePrice)
    {
        if (basePrice <= 0)
        {
            throw new ArgumentException("La tarifa base debe ser mayor a cero.");
        }
    }

    private static void AddRoomTypeParameters(SqlCommand cmd, string name, decimal basePrice)
    {
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = name;
        cmd.Parameters.Add("@BasePrice", SqlDbType.Decimal).Value = basePrice;
        cmd.Parameters["@BasePrice"].Precision = 10;
        cmd.Parameters["@BasePrice"].Scale = 2;
    }

    private static IReadOnlyList<RoomType> DeduplicateRoomTypes(IEnumerable<RoomType> roomTypes)
    {
        return roomTypes
            .GroupBy(roomType => NormalizeRoomTypeKey(roomType.Name))
            .Select(group => group
                .OrderBy(roomType => HasAccents(roomType.Name) ? 0 : 1)
                .ThenBy(roomType => roomType.RoomTypeId)
                .First())
            .OrderBy(roomType => roomType.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static string NormalizeRoomTypeKey(string value)
    {
        return StripDiacritics(value).ToLowerInvariant();
    }

    private static string StripDiacritics(string value)
    {
        var trimmed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(trimmed.Length);

        foreach (var character in trimmed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool HasAccents(string value)
    {
        return !string.Equals(value.Trim(), StripDiacritics(value), StringComparison.Ordinal);
    }

    private static async Task EnsureNameIsAvailableAsync(
        SqlConnection conn,
        string name,
        int? currentRoomTypeId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM RoomType
            WHERE IsActive = 1
              AND LTRIM(RTRIM(Name)) COLLATE Latin1_General_100_CI_AI = LTRIM(RTRIM(@Name)) COLLATE Latin1_General_100_CI_AI
              AND (@CurrentRoomTypeId IS NULL OR RoomTypeId <> @CurrentRoomTypeId);
        """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = name;
        cmd.Parameters.Add("@CurrentRoomTypeId", SqlDbType.Int).Value =
            currentRoomTypeId.HasValue ? currentRoomTypeId.Value : DBNull.Value;

        var matches = (int)await cmd.ExecuteScalarAsync(cancellationToken);
        if (matches > 0)
        {
            throw new InvalidOperationException("Ya existe un tipo de habitación activo con ese nombre.");
        }
    }
}
