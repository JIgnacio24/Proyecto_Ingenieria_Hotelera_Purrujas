using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class PromotionRepository : IPromotionRepository
{
    private readonly string _connectionString;

    public PromotionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return Array.Empty<Promotion>();

        var results = new List<Promotion>();
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            // Admin ve todas las ofertas (activas), con el nombre del tipo de habitación
            const string sql = """
                SELECT
                    p.PromotionId,
                    p.Name,
                    p.Discount,
                    p.StartDate,
                    p.EndDate,
                    p.RoomTypeId,
                    rt.Name AS RoomTypeName,
                    p.IsActive
                FROM Promotion p
                INNER JOIN RoomType rt ON p.RoomTypeId = rt.RoomTypeId
                WHERE p.IsActive = 1
                ORDER BY p.PromotionId DESC
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

    public async Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return null;

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            const string sql = """
                SELECT
                    p.PromotionId,
                    p.Name,
                    p.Discount,
                    p.StartDate,
                    p.EndDate,
                    p.RoomTypeId,
                    rt.Name AS RoomTypeName,
                    p.IsActive
                FROM Promotion p
                INNER JOIN RoomType rt ON p.RoomTypeId = rt.RoomTypeId
                WHERE p.PromotionId = @PromotionId
                  AND p.IsActive = 1
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PromotionId", id);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
                return MapRow(reader);
        }
        catch { /* swallow */ }

        return null;
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    public async Task<Promotion> CreateAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Promotion_Create", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Name", promotion.Name.Trim());
        cmd.Parameters.AddWithValue("@Discount", promotion.Discount);
        cmd.Parameters.AddWithValue("@StartDate", promotion.StartDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@EndDate", promotion.EndDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@RoomTypeId", promotion.RoomTypeId);

        var newId = (int)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);

        return await GetByIdAsync(newId, cancellationToken)
               ?? throw new InvalidOperationException("No se pudo recuperar la oferta creada.");
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    public async Task<Promotion?> UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Promotion_Update", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@PromotionId", promotion.PromotionId);
        cmd.Parameters.AddWithValue("@Name", promotion.Name.Trim());
        cmd.Parameters.AddWithValue("@Discount", promotion.Discount);
        cmd.Parameters.AddWithValue("@StartDate", promotion.StartDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@EndDate", promotion.EndDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@RoomTypeId", promotion.RoomTypeId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return await GetByIdAsync(promotion.PromotionId, cancellationToken);
    }

    // ── DeleteAsync (soft delete) ─────────────────────────────────────────────

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Promotion_Delete", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@PromotionId", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    // ── Mapeo ────────────────────────────────────────────────────────────────

    private static Promotion MapRow(SqlDataReader r) => new()
    {
        PromotionId = r.GetInt32(0),
        Name = r.GetString(1),
        Discount = r.GetInt32(2),
        StartDate = DateOnly.FromDateTime(r.GetDateTime(3)),
        EndDate = DateOnly.FromDateTime(r.GetDateTime(4)),
        RoomTypeId = r.GetInt32(5),
        RoomTypeName = r.GetString(6),
        IsActive = r.GetBoolean(7)
    };
}
