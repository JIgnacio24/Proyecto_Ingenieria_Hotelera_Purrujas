using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly string _connectionString;

    public AdminUserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<AdminUser> RegisterAsync(
        string fullName,
        string username,
        string email,
        string password,
        string role,
        CancellationToken cancellationToken = default)
    {
        EnsureConnectionString();

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand("usp_AdminUser_Register", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@FullName", fullName);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", password);
            cmd.Parameters.AddWithValue("@Role", role);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapAdminUser(reader);
            }

            throw new InvalidOperationException("No fue posible registrar el usuario administrador.");
        }
        catch (SqlException ex)
        {
            throw BuildSqlException(ex);
        }
    }

    public async Task<AdminUser?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        EnsureConnectionString();

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand("usp_AdminUser_Login", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", password);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapAdminUser(reader);
            }

            return null;
        }
        catch (SqlException ex)
        {
            throw BuildSqlException(ex);
        }
    }

    public async Task<AdminUser?> GetByIdAsync(int adminUserId, CancellationToken cancellationToken = default)
    {
        EnsureConnectionString();

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand("usp_AdminUser_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@AdminUserId", adminUserId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapAdminUser(reader);
            }

            return null;
        }
        catch (SqlException ex)
        {
            throw BuildSqlException(ex);
        }
    }

    private void EnsureConnectionString()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("La cadena de conexión DefaultConnection no está configurada.");
        }
    }

    private InvalidOperationException BuildSqlException(SqlException ex)
    {
        var server = "SQL Server";

        try
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            server = string.IsNullOrWhiteSpace(builder.DataSource) ? server : builder.DataSource;
        }
        catch
        {
            // Keep fallback label.
        }

        var isConnectionIssue =
            ex.Message.Contains("No se pudo abrir una conexión con SQL Server", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("No se encontró el servidor", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Cannot generate SSPI context", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("No se puede generar contexto SSPI", StringComparison.OrdinalIgnoreCase);

        if (!isConnectionIssue)
        {
            return new InvalidOperationException(ex.Message, ex);
        }

        var message =
            $"No se pudo conectar a SQL Server usando '{server}'. " +
            "Revisa la variable ConnectionStrings__DefaultConnection en src/Api/.env. " +
            "En esta maquina hay instancias 'localhost\\SQLEXPRESS' y 'localhost\\SQLEXPRESS01'. " +
            "Usa la misma instancia donde ejecutaste DB-Ingenieria-Purrujas.sql.";

        return new InvalidOperationException(message, ex);
    }

    private static AdminUser MapAdminUser(SqlDataReader reader)
    {
        var adminUserIdOrdinal = reader.GetOrdinal("AdminUserId");
        var fullNameOrdinal = reader.GetOrdinal("FullName");
        var usernameOrdinal = reader.GetOrdinal("Username");
        var emailOrdinal = reader.GetOrdinal("Email");
        var roleOrdinal = reader.GetOrdinal("Role");
        var isActiveOrdinal = reader.GetOrdinal("IsActive");
        var createdAtOrdinal = reader.GetOrdinal("CreatedAt");
        var lastLoginAtOrdinal = reader.GetOrdinal("LastLoginAt");

        return new AdminUser
        {
            AdminUserId = reader.GetInt32(adminUserIdOrdinal),
            FullName = reader.GetString(fullNameOrdinal),
            Username = reader.GetString(usernameOrdinal),
            Email = reader.GetString(emailOrdinal),
            Role = reader.GetString(roleOrdinal),
            IsActive = reader.GetBoolean(isActiveOrdinal),
            CreatedAt = reader.GetDateTime(createdAtOrdinal),
            LastLoginAt = reader.IsDBNull(lastLoginAtOrdinal)
                ? null
                : reader.GetDateTime(lastLoginAtOrdinal)
        };
    }
}
