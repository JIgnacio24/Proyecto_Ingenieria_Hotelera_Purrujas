using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class AdminAuditLogRepository : IAdminAuditLogRepository
{
    private readonly string _connectionString;

    public AdminAuditLogRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<AdminAuditLog> CreateAsync(
        int adminUserId,
        string action,
        string description,
        CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_AdminAuditLog_Create", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 8
        };

        cmd.Parameters.Add("@AdminUserId", SqlDbType.Int).Value = adminUserId;
        cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 150).Value = action;
        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 600).Value = description;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapAdminAuditLog(reader);
        }

        throw new InvalidOperationException("No fue posible registrar la bitacora administrativa.");
    }

    public async Task<IReadOnlyList<AdminAuditLog>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_AdminAuditLog_GetRecent", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 8
        };

        cmd.Parameters.Add("@Take", SqlDbType.Int).Value = take;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var logs = new List<AdminAuditLog>();

        while (await reader.ReadAsync(cancellationToken))
        {
            logs.Add(MapAdminAuditLog(reader));
        }

        return logs;
    }

    private SqlConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("No se configuro la cadena de conexion de SQL Server.");
        }

        return new SqlConnection(_connectionString);
    }

    private static AdminAuditLog MapAdminAuditLog(SqlDataReader reader)
    {
        return new AdminAuditLog
        {
            AdminAuditLogId = reader.GetInt32(reader.GetOrdinal("AdminAuditLogId")),
            AdminUserId = reader.GetInt32(reader.GetOrdinal("AdminUserId")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            OccurredAt = reader.GetDateTime(reader.GetOrdinal("OccurredAt")),
            Action = reader.GetString(reader.GetOrdinal("Action")),
            Description = reader.GetString(reader.GetOrdinal("Description"))
        };
    }
}
