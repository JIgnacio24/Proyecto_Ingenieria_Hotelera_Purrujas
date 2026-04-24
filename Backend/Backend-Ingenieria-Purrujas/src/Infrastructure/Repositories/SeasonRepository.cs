using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class SeasonRepository : ISeasonRepository
{
    private readonly string _connectionString;

    public SeasonRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<IReadOnlyCollection<Season>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return Array.Empty<Season>();
        }

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
            {
                results.Add(new Season
                {
                    SeasonId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    PercentageChange = reader.GetInt32(2),
                    StartDate = DateOnly.FromDateTime(reader.GetDateTime(3)),
                    EndDate = DateOnly.FromDateTime(reader.GetDateTime(4)),
                    IsActive = reader.GetBoolean(5)
                });
            }
        }
        catch
        {
            // swallow; fallback empty
        }

        return results;
    }
}
