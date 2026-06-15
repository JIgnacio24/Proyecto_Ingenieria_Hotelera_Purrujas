using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class OccupancyRepository : IOccupancyRepository
{
    private readonly string _connectionString;

    public OccupancyRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<IEnumerable<OccupancyRecord>> GetHistoryAsync()
    {
        var results = new List<OccupancyRecord>();
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = """
                SELECT Year, Month, OccupancyPercentage
                FROM OccupancyHistory
                ORDER BY Year ASC, Month ASC
                """;

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new OccupancyRecord
                {
                    Year = reader.GetInt32(0),
                    Month = reader.GetInt32(1),
                    OccupancyPercentage = (float)reader.GetDecimal(2)
                });
            }
        }
        catch { /* fallback vacío */ }

        return results;
    }

    public async Task<bool> HasDataAsync()
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = "SELECT TOP 1 1 FROM OccupancyHistory";
            await using var cmd = new SqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();
            return result is not null;
        }
        catch
        {
            return false;
        }
    }
}
