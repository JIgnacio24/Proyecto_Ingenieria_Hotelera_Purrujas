using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class AdvertisingRepository : IAdvertisingRepository
{
    private readonly string _connectionString;

    public AdvertisingRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<IReadOnlyList<Advertising>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return Array.Empty<Advertising>();

        var results = new List<Advertising>();
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand("usp_Advertising_GetAll", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                results.Add(MapRow(reader));
        }
        catch { /* swallow; fallback vacío */ }

        return results;
    }

    public async Task<IReadOnlyList<Advertising>> GetAllAdminAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return Array.Empty<Advertising>();

        var results = new List<Advertising>();
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand("usp_Advertising_GetAll_Admin", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                results.Add(MapRow(reader));
        }
        catch { /* swallow; fallback vacío */ }

        return results;
    }

    public async Task<Advertising?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return null;

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand("usp_Advertising_GetById", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@AdvertisingId", id);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
                return MapRow(reader);
        }
        catch { /* swallow */ }

        return null;
    }

    public async Task<Advertising> CreateAsync(Advertising advertising, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand("usp_Advertising_Create", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Title",       advertising.Title.Trim());
        cmd.Parameters.AddWithValue("@Description", (object?)advertising.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ImageUrl",    (object?)advertising.ImageUrl    ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Link",        advertising.Link.Trim());
        cmd.Parameters.AddWithValue("@IsActive",    advertising.IsActive);

        var newId = (int)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);

        return await GetByIdAsync(newId, cancellationToken)
               ?? throw new InvalidOperationException("No se pudo recuperar el anuncio creado.");
    }

    public async Task<Advertising?> UpdateAsync(Advertising advertising, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand("usp_Advertising_Update", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@AdvertisingId", advertising.AdvertisingId);
        cmd.Parameters.AddWithValue("@Title",         advertising.Title.Trim());
        cmd.Parameters.AddWithValue("@Description",   (object?)advertising.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ImageUrl",      (object?)advertising.ImageUrl    ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Link",          advertising.Link.Trim());
        cmd.Parameters.AddWithValue("@IsActive",      advertising.IsActive);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return await GetByIdAsync(advertising.AdvertisingId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand("usp_Advertising_Delete", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@AdvertisingId", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static Advertising MapRow(SqlDataReader r) => new()
    {
        AdvertisingId = r.GetInt32(0),
        Title         = r.GetString(1),
        Description   = r.IsDBNull(2) ? null : r.GetString(2),
        ImageUrl      = r.IsDBNull(3) ? null : r.GetString(3),
        Link          = r.GetString(4),
        IsActive      = r.GetBoolean(5),
        CreatedAt     = r.GetDateTime(6),
        UpdatedAt     = r.IsDBNull(7) ? null : r.GetDateTime(7)
    };
}
