using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class SeasonRepository : ISeasonRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SeasonRepository> _logger;

    public SeasonRepository(IConfiguration configuration, ILogger<SeasonRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        _logger = logger;
    }

    // ── GetActiveAsync — usado por el motor de cotización ────────────────────

    public async Task<IReadOnlyCollection<Season>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return Array.Empty<Season>();

        var results = new List<Season>();
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = """
                SELECT SeasonId, Name, PercentageChange, StartDate, EndDate, IsActive
                FROM Season
                WHERE IsActive = 1
                ORDER BY StartDate ASC
                """;

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                results.Add(MapRow(reader));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener las temporadas activas; se devuelve lista vacía.");
        }

        return results;
    }

    // ── GetAllAsync — admin: devuelve todas (activas e inactivas) ────────────

    public async Task<IReadOnlyList<Season>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return Array.Empty<Season>();

        var results = new List<Season>();
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = """
                SELECT SeasonId, Name, PercentageChange, StartDate, EndDate, IsActive
                FROM Season
                ORDER BY IsActive DESC, StartDate ASC, SeasonId DESC
                """;

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                results.Add(MapRow(reader));
        }
        catch { /* swallow; fallback empty */ }

        return results;
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    public async Task<Season?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return null;

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = """
                SELECT SeasonId, Name, PercentageChange, StartDate, EndDate, IsActive
                FROM Season
                WHERE SeasonId = @SeasonId
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SeasonId", id);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
                return MapRow(reader);
        }
        catch { /* swallow */ }

        return null;
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    public async Task<Season> CreateAsync(Season season, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Season_Create", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Name", season.Name.Trim());
        cmd.Parameters.AddWithValue("@PercentageChange", season.PercentageChange);
        cmd.Parameters.AddWithValue("@StartDate", season.StartDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@EndDate", season.EndDate.ToString("yyyy-MM-dd"));

        var newId = (int)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);

        return await GetByIdAsync(newId, cancellationToken)
               ?? throw new InvalidOperationException("No se pudo recuperar la temporada creada.");
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    public async Task<Season?> UpdateAsync(Season season, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Season_Update", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@SeasonId", season.SeasonId);
        cmd.Parameters.AddWithValue("@Name", season.Name.Trim());
        cmd.Parameters.AddWithValue("@PercentageChange", season.PercentageChange);
        cmd.Parameters.AddWithValue("@StartDate", season.StartDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@EndDate", season.EndDate.ToString("yyyy-MM-dd"));

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return await GetByIdAsync(season.SeasonId, cancellationToken);
    }

    // ── DeleteAsync (soft delete) ─────────────────────────────────────────────

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Season_Delete", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@SeasonId", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    // ── Mapeo ────────────────────────────────────────────────────────────────

    private static Season MapRow(SqlDataReader r) => new()
    {
        SeasonId = r.GetInt32(0),
        Name = r.GetString(1),
        PercentageChange = r.GetInt32(2),
        StartDate = DateOnly.FromDateTime(r.GetDateTime(3)),
        EndDate = DateOnly.FromDateTime(r.GetDateTime(4)),
        IsActive = r.GetBoolean(5)
    };
}
