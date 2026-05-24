using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class RoomTypeRepository : IRoomTypeRepository
{
    private readonly string _connectionString;
    private static readonly Dictionary<string, RoomType> FallbackRoomTypes = new()
    {
        { "doble", new RoomType { RoomTypeId = 1, Name = "Habitación Doble", BasePrice = 95 } },
        { "suite", new RoomType { RoomTypeId = 2, Name = "Suite Volcán", BasePrice = 135 } },
        { "villa", new RoomType { RoomTypeId = 3, Name = "Villa Familiar", BasePrice = 180 } }
    };

    public RoomTypeRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<IReadOnlyList<RoomType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            SELECT RoomTypeId, Name, BasePrice, IsActive
            FROM RoomType
            WHERE IsActive = 1
            ORDER BY Name ASC;
        """;

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var roomTypes = new List<RoomType>();

        while (await reader.ReadAsync(cancellationToken))
        {
            roomTypes.Add(MapRoomType(reader));
        }

        return roomTypes;
    }

    public async Task<RoomType?> GetByIdAsync(int roomTypeId, CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            SELECT RoomTypeId, Name, BasePrice, IsActive
            FROM RoomType
            WHERE RoomTypeId = @RoomTypeId
              AND IsActive = 1;
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
                SELECT TOP 1 RoomTypeId, Name, BasePrice, IsActive
                FROM RoomType
                WHERE IsActive = 1
                  AND (
                    LOWER(Name) = LOWER(@nameKey)
                    OR LOWER(Name) LIKE '%' + LOWER(@nameKey) + '%'
                  )
            """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nameKey", roomKey);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapRoomType(reader);
            }
        }
        catch
        {
            // Fallback below
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
            INSERT INTO RoomType (Name, BasePrice, IsActive)
            OUTPUT INSERTED.RoomTypeId, INSERTED.Name, INSERTED.BasePrice, INSERTED.IsActive
            VALUES (@Name, @BasePrice, 1);
        """;

        await using var cmd = new SqlCommand(sql, conn);
        AddRoomTypeParameters(cmd, normalizedName, roomType.BasePrice);

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
                BasePrice = @BasePrice
            OUTPUT INSERTED.RoomTypeId, INSERTED.Name, INSERTED.BasePrice, INSERTED.IsActive
            WHERE RoomTypeId = @RoomTypeId
              AND IsActive = 1;
        """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@RoomTypeId", SqlDbType.Int).Value = roomType.RoomTypeId;
        AddRoomTypeParameters(cmd, normalizedName, roomType.BasePrice);

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
            RoomTypeId = reader.GetInt32(0),
            Name = reader.GetString(1),
            BasePrice = reader.GetDecimal(2),
            IsActive = reader.GetBoolean(3)
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
              AND LOWER(Name) = LOWER(@Name)
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
