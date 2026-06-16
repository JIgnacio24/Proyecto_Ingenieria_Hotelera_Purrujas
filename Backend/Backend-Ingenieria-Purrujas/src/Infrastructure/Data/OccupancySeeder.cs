using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Data;

public class OccupancySeeder
{
    private readonly string _connectionString;

    // Ocupación mensual base (%) cuando no hay temporada configurada
    private static readonly Dictionary<int, double> DefaultOccupancyByMonth = new()
    {
        { 1, 65 }, { 2, 68 }, { 3, 72 }, { 4, 75 }, { 5, 70 },
        { 6, 80 }, { 7, 88 }, { 8, 85 }, { 9, 70 }, { 10, 65 },
        { 11, 62 }, { 12, 82 }
    };

    public OccupancySeeder(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task SeedAsync()
    {
        await EnsureTableExistsAsync();

        if (await HasDataAsync())
            return;

        var seasons = await GetActiveSeasonsAsync();
        var records = GenerateSyntheticData(seasons);
        await InsertRecordsAsync(records);
    }

    private async Task EnsureTableExistsAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = """
            IF NOT EXISTS (
                SELECT 1 FROM sys.tables WHERE name = 'OccupancyHistory'
            )
            BEGIN
                CREATE TABLE OccupancyHistory (
                    Id                  INT IDENTITY(1,1) PRIMARY KEY,
                    Year                INT NOT NULL,
                    Month               INT NOT NULL CHECK (Month BETWEEN 1 AND 12),
                    OccupancyPercentage DECIMAL(5,2) NOT NULL,
                    CONSTRAINT UQ_OccupancyHistory_YearMonth UNIQUE (Year, Month)
                );
            END
            """;

        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<bool> HasDataAsync()
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("SELECT TOP 1 1 FROM OccupancyHistory", conn);
            return await cmd.ExecuteScalarAsync() is not null;
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<(int PercentageChange, DateOnly StartDate, DateOnly EndDate)>> GetActiveSeasonsAsync()
    {
        var seasons = new List<(int, DateOnly, DateOnly)>();
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = """
                SELECT PercentageChange, StartDate, EndDate
                FROM Season
                WHERE IsActive = 1
                """;

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                seasons.Add((
                    reader.GetInt32(0),
                    DateOnly.FromDateTime(reader.GetDateTime(1)),
                    DateOnly.FromDateTime(reader.GetDateTime(2))
                ));
            }
        }
        catch { /* Si la tabla Season aún no existe, se usa el fallback */ }

        return seasons;
    }

    private static List<(int Year, int Month, double OccupancyPercentage)> GenerateSyntheticData(
        List<(int PercentageChange, DateOnly StartDate, DateOnly EndDate)> seasons)
    {
        var rng = new Random(42); // semilla fija para reproducibilidad
        var records = new List<(int, int, double)>();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = today.AddMonths(-36);

        for (int i = 0; i < 36; i++)
        {
            var current = start.AddMonths(i);
            var midpoint = new DateOnly(current.Year, current.Month, 15);

            var matchingSeason = seasons.FirstOrDefault(s =>
                s.StartDate <= midpoint && s.EndDate >= midpoint);

            double baseOccupancy;
            if (matchingSeason != default)
            {
                // Fórmula: a mayor PercentageChange (precio), mayor demanda/ocupación
                baseOccupancy = 60.0 + matchingSeason.PercentageChange * 1.5;
            }
            else
            {
                baseOccupancy = DefaultOccupancyByMonth[current.Month];
            }

            // Variación aleatoria de ±5 puntos porcentuales
            double noise = (rng.NextDouble() * 10.0) - 5.0;
            double occupancy = Math.Clamp(baseOccupancy + noise, 40.0, 95.0);
            occupancy = Math.Round(occupancy, 2);

            records.Add((current.Year, current.Month, occupancy));
        }

        return records;
    }

    private async Task InsertRecordsAsync(List<(int Year, int Month, double OccupancyPercentage)> records)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = """
            INSERT INTO OccupancyHistory (Year, Month, OccupancyPercentage)
            VALUES (@Year, @Month, @OccupancyPercentage)
            """;

        foreach (var (year, month, pct) in records)
        {
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Year", year);
            cmd.Parameters.AddWithValue("@Month", month);
            cmd.Parameters.AddWithValue("@OccupancyPercentage", (decimal)pct);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
