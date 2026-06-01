using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly string _connectionString;

    public RoomRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<Room?> GetFirstAvailableAsync(string roomTypeKey, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand("usp_Room_GetFirstAvailableByTypeKey", conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 8
            };
            cmd.Parameters.AddWithValue("@RoomTypeKey", roomTypeKey);
            cmd.Parameters.AddWithValue("@StartDate", startDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@EndDate", endDate.ToDateTime(TimeOnly.MinValue));

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new Room
                {
                    RoomId       = reader.GetInt32(reader.GetOrdinal("RoomId")),
                    RoomNumber   = reader.GetString(reader.GetOrdinal("RoomNumber")),
                    RoomTypeName = reader.GetString(reader.GetOrdinal("RoomTypeName")),
                    BasePrice    = reader.GetDecimal(reader.GetOrdinal("BasePrice"))
                };
            }
            return null;
        }
        catch (SqlException ex) when (ex.Number == 50030)
        {
            return null;
        }
    }

    public async Task<int> CountAvailableAsync(string roomTypeKey, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand("usp_Room_CountAvailableByTypeKey", conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 8
            };
            cmd.Parameters.AddWithValue("@RoomTypeKey", roomTypeKey);
            cmd.Parameters.AddWithValue("@StartDate", startDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@EndDate", endDate.ToDateTime(TimeOnly.MinValue));

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return reader.GetInt32(reader.GetOrdinal("AvailableCount"));
            }
            return 0;
        }
        catch (SqlException)
        {
            throw;
        }
    }

    public async Task<string> GetRoomTypeNameAsync(string roomTypeKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            // Use count SP which also returns room type name
            await using var cmd = new SqlCommand("usp_Room_CountAvailableByTypeKey", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            var today = DateOnly.FromDateTime(DateTime.Today);
            cmd.Parameters.AddWithValue("@RoomTypeKey", roomTypeKey);
            cmd.Parameters.AddWithValue("@StartDate", today.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@EndDate", today.AddDays(1).ToDateTime(TimeOnly.MinValue));

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var name = reader.GetString(reader.GetOrdinal("RoomTypeName"));
                return string.IsNullOrWhiteSpace(name) ? roomTypeKey : name;
            }
        }
        catch { /* fallback below */ }

        return roomTypeKey switch
        {
            "doble" => "Habitación Doble",
            "suite" => "Suite Volcán",
            "villa" => "Villa Familiar",
            _       => roomTypeKey
        };
    }

    public async Task<string?> GetRoomTypeKeyByRoomIdAsync(int roomId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            SELECT LOWER(rt.Name)
            FROM Room r
            INNER JOIN RoomType rt ON rt.RoomTypeId = r.RoomTypeId
            WHERE r.RoomId = @RoomId AND r.IsActive = 1
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RoomId", roomId);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is string key ? key : null;
    }
}
