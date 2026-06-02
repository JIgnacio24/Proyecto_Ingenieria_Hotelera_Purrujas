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

    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Room_GetAll", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 8
        };

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var rooms = new List<Room>();

        while (await reader.ReadAsync(cancellationToken))
        {
            rooms.Add(MapRoom(reader));
        }

        return rooms;
    }

    public async Task<Room?> GetByIdAsync(int roomId, CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Room_GetById", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 8
        };
        cmd.Parameters.AddWithValue("@RoomId", roomId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRoom(reader) : null;
    }

    public async Task<Room> CreateAsync(Room room, CancellationToken cancellationToken = default)
    {
        ValidateRoomNumber(room.RoomNumber);

        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        const string existingSql = """
            SELECT TOP 1 RoomId
            FROM Room
            WHERE RoomNumber = @RoomNumber
              AND IsActive = 0
            ORDER BY RoomId DESC;
        """;

        await using (var activeCheckCmd = new SqlCommand("""
            SELECT COUNT(1)
            FROM Room
            WHERE RoomNumber = @RoomNumber
              AND IsActive = 1;
            """, conn))
        {
            activeCheckCmd.Parameters.AddWithValue("@RoomNumber", room.RoomNumber.Trim());

            var activeCount = Convert.ToInt32(await activeCheckCmd.ExecuteScalarAsync(cancellationToken));
            if (activeCount > 0)
            {
                throw new InvalidOperationException("El número de habitación ya existe.");
            }
        }

        await using (var existingInactiveCmd = new SqlCommand(existingSql, conn))
        {
            existingInactiveCmd.Parameters.AddWithValue("@RoomNumber", room.RoomNumber.Trim());
            var existingRoomId = await existingInactiveCmd.ExecuteScalarAsync(cancellationToken);

            if (existingRoomId is not null && existingRoomId != DBNull.Value)
            {
                var inactiveRoomId = Convert.ToInt32(existingRoomId);
                await using var updateCmd = new SqlCommand("""
                    UPDATE Room
                    SET IsActive = 1,
                        RoomTypeId = @RoomTypeId,
                        RoomStatusId = @RoomStatusId
                    WHERE RoomId = @RoomId;
                    """, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 8
                };
                updateCmd.Parameters.AddWithValue("@RoomId", inactiveRoomId);
                updateCmd.Parameters.AddWithValue("@RoomTypeId", room.RoomTypeId);
                updateCmd.Parameters.AddWithValue("@RoomStatusId", room.RoomStatusId);

                var affectedRows = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                if (affectedRows <= 0)
                {
                    throw new InvalidOperationException("No fue posible reactivar la habitación.");
                }

                return await GetByIdAsync(inactiveRoomId, cancellationToken)
                    ?? throw new InvalidOperationException("No fue posible obtener la habitación creada.");
            }
        }

        await using (var insertCmd = new SqlCommand("""
            INSERT INTO Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId)
            VALUES (@RoomNumber, 1, @RoomTypeId, @RoomStatusId);
            SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewRoomId;
            """, conn)
        {
            CommandType = CommandType.Text,
            CommandTimeout = 8
        })
        {
            insertCmd.Parameters.AddWithValue("@RoomNumber", room.RoomNumber.Trim());
            insertCmd.Parameters.AddWithValue("@RoomTypeId", room.RoomTypeId);
            insertCmd.Parameters.AddWithValue("@RoomStatusId", room.RoomStatusId);

            var result = await insertCmd.ExecuteScalarAsync(cancellationToken);
            if (result is null || result == DBNull.Value)
            {
                throw new InvalidOperationException("No fue posible crear la habitación.");
            }

            var newRoomId = Convert.ToInt32(result);
            return await GetByIdAsync(newRoomId, cancellationToken)
                ?? throw new InvalidOperationException("No fue posible obtener la habitación creada.");
        }
    }

    public async Task<Room?> UpdateAsync(Room room, CancellationToken cancellationToken = default)
    {
        ValidateRoomNumber(room.RoomNumber);

        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        await using (var cmd = new SqlCommand("usp_Room_Update", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 8
        })
        {
            cmd.Parameters.AddWithValue("@RoomId", room.RoomId);
            cmd.Parameters.AddWithValue("@RoomNumber", room.RoomNumber.Trim());
            cmd.Parameters.AddWithValue("@RoomTypeId", room.RoomTypeId);
            cmd.Parameters.AddWithValue("@RoomStatusId", room.RoomStatusId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapRoom(reader);
        }
    }

    public async Task<bool> DeleteAsync(int roomId, CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Room_Delete", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 8
        };
        cmd.Parameters.AddWithValue("@RoomId", roomId);

        var affectedRows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0;
    }

    public async Task<IReadOnlyList<RoomStatusOption>> GetRoomStatusesAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            SELECT RoomStatusId, Name, Description, IsAvailableForBooking
            FROM RoomStatus
            ORDER BY Name ASC;
        """;

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var statuses = new List<RoomStatusOption>();

        while (await reader.ReadAsync(cancellationToken))
        {
            statuses.Add(new RoomStatusOption
            {
                RoomStatusId = reader.GetInt32(reader.GetOrdinal("RoomStatusId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                IsAvailableForBooking = reader.GetBoolean(reader.GetOrdinal("IsAvailableForBooking"))
            });
        }

        return statuses;
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

    private SqlConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("No se configuró la cadena de conexión de SQL Server.");
        }

        return new SqlConnection(_connectionString);
    }

    private static Room MapRoom(SqlDataReader reader)
    {
        return new Room
        {
            RoomId = reader.GetInt32(reader.GetOrdinal("RoomId")),
            RoomNumber = reader.GetString(reader.GetOrdinal("RoomNumber")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            RoomTypeId = reader.GetInt32(reader.GetOrdinal("RoomTypeId")),
            RoomTypeName = reader.GetString(reader.GetOrdinal("RoomTypeName")),
            RoomStatusId = reader.GetInt32(reader.GetOrdinal("RoomStatusId")),
            RoomStatusName = reader.GetString(reader.GetOrdinal("RoomStatusName"))
        };
    }

    private static void ValidateRoomNumber(string roomNumber)
    {
        if (string.IsNullOrWhiteSpace(roomNumber))
        {
            throw new ArgumentException("El número de habitación es obligatorio.");
        }

        if (roomNumber.Trim().Length > 50)
        {
            throw new ArgumentException("El número de habitación no puede superar 50 caracteres.");
        }
    }
}
