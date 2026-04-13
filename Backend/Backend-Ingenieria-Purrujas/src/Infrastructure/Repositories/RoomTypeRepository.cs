using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

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
                SELECT TOP 1 RoomTypeId, Name, BasePrice
                FROM RoomType
                WHERE LOWER(Name) = LOWER(@nameKey)
                   OR LOWER(Name) LIKE '%' + LOWER(@nameKey) + '%'
            """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nameKey", roomKey);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new RoomType
                {
                    RoomTypeId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    BasePrice = reader.GetDecimal(2)
                };
            }
        }
        catch
        {
            // Fallback below
        }

        return TryGetFallback(roomKey);
    }

    private static RoomType? TryGetFallback(string roomKey)
    {
        return FallbackRoomTypes.TryGetValue(roomKey.ToLowerInvariant(), out var roomType)
            ? roomType
            : null;
    }
}
