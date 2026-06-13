using Backend_Ingenieria_Purrujas.Domain.Dashboard;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly string _connectionString;

    public DashboardRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<DashboardMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var today      = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var kpi            = await GetKpiAsync(conn, today, monthStart, cancellationToken);
        var byMonth        = await GetReservationsByMonthAsync(conn, monthStart, cancellationToken);
        var revenueByMonth = await GetRevenueByMonthAsync(conn, monthStart, cancellationToken);
        var byStatus       = await GetByStatusAsync(conn, cancellationToken);
        var roomStatus     = await GetRoomStatusAsync(conn, today, cancellationToken);
        var topRoomTypes   = await GetTopRoomTypesAsync(conn, cancellationToken);

        return new DashboardMetrics
        {
            Kpi                  = kpi,
            ReservationsByMonth  = byMonth,
            RevenueByMonth       = revenueByMonth,
            ReservationsByStatus = byStatus,
            RoomStatus           = roomStatus,
            TopRoomTypes         = topRoomTypes
        };
    }

    // ── KPIs ──────────────────────────────────────────────────────────────────

    private static async Task<KpiSummary> GetKpiAsync(
        SqlConnection conn, DateTime today, DateTime monthStart, CancellationToken ct)
    {
        // OccupiedRooms: rooms with a Confirmada/Pendiente reservation spanning today.
        // AvailableRooms: rooms flagged 'Disponible' that are NOT occupied today.
        const string sql = """
            SELECT
                (SELECT COUNT(*)
                 FROM Reservation
                 WHERE IsActive = 1
                   AND CAST(ReservationDate AS DATE) >= @MonthStart)             AS ReservationsThisMonth,

                (SELECT COUNT(*)
                 FROM Reservation r
                 INNER JOIN ReservationStatus rs
                     ON rs.ReservationStatusId = r.ReservationStatusId
                 WHERE r.IsActive = 1
                   AND rs.Name = 'Confirmada'
                   AND CAST(r.ReservationDate AS DATE) >= @MonthStart)           AS ConfirmedThisMonth,

                (SELECT ISNULL(SUM(b.BasePrice + b.SeasonAmount - b.Discount), 0)
                 FROM Reservation r
                 INNER JOIN ReservationStatus rs
                     ON rs.ReservationStatusId = r.ReservationStatusId
                 INNER JOIN Bill b
                     ON b.ReservationId = r.ReservationId
                 WHERE r.IsActive = 1
                   AND rs.Name = 'Confirmada'
                   AND CAST(r.ReservationDate AS DATE) >= @MonthStart)           AS RevenueThisMonthUsd,

                (SELECT COUNT(*) FROM Room WHERE IsActive = 1)                   AS TotalRooms,

                (SELECT COUNT(DISTINCT r.RoomId)
                 FROM Reservation r
                 INNER JOIN ReservationStatus rs
                     ON rs.ReservationStatusId = r.ReservationStatusId
                 WHERE r.IsActive = 1
                   AND rs.Name IN ('Confirmada', 'Pendiente')
                   AND CAST(r.StartDate AS DATE) <= @Today
                   AND CAST(r.EndDate   AS DATE) >= @Today)                      AS OccupiedRooms,

                (SELECT COUNT(*)
                 FROM Room rm
                 INNER JOIN RoomStatus rms
                     ON rms.RoomStatusId = rm.RoomStatusId
                 WHERE rm.IsActive = 1
                   AND rms.Name = 'Disponible'
                   AND rm.RoomId NOT IN (
                       SELECT r2.RoomId
                       FROM Reservation r2
                       INNER JOIN ReservationStatus rs2
                           ON rs2.ReservationStatusId = r2.ReservationStatusId
                       WHERE r2.IsActive = 1
                         AND rs2.Name IN ('Confirmada', 'Pendiente')
                         AND CAST(r2.StartDate AS DATE) <= @Today
                         AND CAST(r2.EndDate   AS DATE) >= @Today
                   ))                                                            AS AvailableRooms
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Today",      today);
        cmd.Parameters.AddWithValue("@MonthStart", monthStart);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct)) return new KpiSummary();

        return new KpiSummary
        {
            ReservationsThisMonth = reader.GetInt32(0),
            ConfirmedThisMonth    = reader.GetInt32(1),
            RevenueThisMonthUsd   = reader.GetDecimal(2),
            TotalRooms            = reader.GetInt32(3),
            OccupiedRooms         = reader.GetInt32(4),
            AvailableRooms        = reader.GetInt32(5)
        };
    }

    // ── Reservations by month (last 6 months including current) ──────────────

    private static async Task<IReadOnlyList<MonthlyDataPoint>> GetReservationsByMonthAsync(
        SqlConnection conn, DateTime monthStart, CancellationToken ct)
    {
        const string sql = """
            WITH Months AS (
                SELECT @SixMonthsAgo AS MonthStart
                UNION ALL
                SELECT DATEADD(MONTH, 1, MonthStart) FROM Months
                WHERE MonthStart < @NextMonth
            )
            SELECT
                YEAR(m.MonthStart)          AS Year,
                MONTH(m.MonthStart)         AS Month,
                COUNT(r.ReservationId)      AS Count
            FROM Months m
            LEFT JOIN Reservation r
                ON  YEAR(r.ReservationDate)  = YEAR(m.MonthStart)
                AND MONTH(r.ReservationDate) = MONTH(m.MonthStart)
                AND r.IsActive = 1
            GROUP BY m.MonthStart, YEAR(m.MonthStart), MONTH(m.MonthStart)
            ORDER BY m.MonthStart
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SixMonthsAgo", monthStart.AddMonths(-5));
        cmd.Parameters.AddWithValue("@NextMonth",    monthStart.AddMonths(1));
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var results = new List<MonthlyDataPoint>();
        while (await reader.ReadAsync(ct))
            results.Add(new MonthlyDataPoint { Year = reader.GetInt32(0), Month = reader.GetInt32(1), Count = reader.GetInt32(2) });

        return results.AsReadOnly();
    }

    // ── Revenue by month (confirmed only, last 6 months including current) ───

    private static async Task<IReadOnlyList<MonthlyDataPoint>> GetRevenueByMonthAsync(
        SqlConnection conn, DateTime monthStart, CancellationToken ct)
    {
        const string sql = """
            WITH Months AS (
                SELECT @SixMonthsAgo AS MonthStart
                UNION ALL
                SELECT DATEADD(MONTH, 1, MonthStart) FROM Months
                WHERE MonthStart < @NextMonth
            )
            SELECT
                YEAR(m.MonthStart)  AS Year,
                MONTH(m.MonthStart) AS Month,
                ISNULL((
                    SELECT SUM(b.BasePrice + b.SeasonAmount - b.Discount)
                    FROM Reservation r
                    INNER JOIN ReservationStatus rs
                        ON rs.ReservationStatusId = r.ReservationStatusId
                    INNER JOIN Bill b
                        ON b.ReservationId = r.ReservationId
                    WHERE r.IsActive = 1
                      AND rs.Name = 'Confirmada'
                      AND YEAR(r.ReservationDate)  = YEAR(m.MonthStart)
                      AND MONTH(r.ReservationDate) = MONTH(m.MonthStart)
                ), 0)               AS Amount
            FROM Months m
            ORDER BY m.MonthStart
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SixMonthsAgo", monthStart.AddMonths(-5));
        cmd.Parameters.AddWithValue("@NextMonth",    monthStart.AddMonths(1));
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var results = new List<MonthlyDataPoint>();
        while (await reader.ReadAsync(ct))
            results.Add(new MonthlyDataPoint { Year = reader.GetInt32(0), Month = reader.GetInt32(1), Amount = reader.GetDecimal(2) });

        return results.AsReadOnly();
    }

    // ── Reservations by status ────────────────────────────────────────────────

    private static async Task<IReadOnlyList<StatusCount>> GetByStatusAsync(
        SqlConnection conn, CancellationToken ct)
    {
        const string sql = """
            SELECT rs.Name AS Status, COUNT(*) AS Count
            FROM Reservation r
            INNER JOIN ReservationStatus rs ON rs.ReservationStatusId = r.ReservationStatusId
            WHERE r.IsActive = 1
            GROUP BY rs.Name
            ORDER BY Count DESC
            """;

        await using var cmd    = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var results = new List<StatusCount>();
        while (await reader.ReadAsync(ct))
            results.Add(new StatusCount { Status = reader.GetString(0), Count = reader.GetInt32(1) });

        return results.AsReadOnly();
    }

    // ── Room status (dynamic, based on today's active reservations) ───────────

    private static async Task<RoomStatusSummary> GetRoomStatusAsync(
        SqlConnection conn, DateTime today, CancellationToken ct)
    {
        // SQL Server forbids aggregate functions whose argument expression contains a subquery.
        // Solution: pre-compute which rooms are occupied via a LEFT JOIN subquery, then SUM
        // on the simple join-result expression (occ.RoomId IS NOT NULL).
        const string sql = """
            SELECT
                ISNULL(SUM(CASE WHEN occ.RoomId IS NOT NULL THEN 1 ELSE 0 END), 0)                           AS Occupied,
                ISNULL(SUM(CASE WHEN rms.Name = 'Disponible'  AND occ.RoomId IS NULL THEN 1 ELSE 0 END), 0)  AS Available,
                ISNULL(SUM(CASE WHEN rms.Name <> 'Disponible' AND occ.RoomId IS NULL THEN 1 ELSE 0 END), 0)  AS Maintenance,
                COUNT(*)                                                                                       AS Total
            FROM Room rm
            INNER JOIN RoomStatus rms ON rms.RoomStatusId = rm.RoomStatusId
            LEFT JOIN (
                SELECT DISTINCT r.RoomId
                FROM Reservation r
                INNER JOIN ReservationStatus rs ON rs.ReservationStatusId = r.ReservationStatusId
                WHERE r.IsActive = 1
                  AND rs.Name IN ('Confirmada', 'Pendiente')
                  AND CAST(r.StartDate AS DATE) <= @Today
                  AND CAST(r.EndDate   AS DATE) >= @Today
            ) AS occ ON occ.RoomId = rm.RoomId
            WHERE rm.IsActive = 1
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Today", today);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct)) return new RoomStatusSummary();

        return new RoomStatusSummary
        {
            Occupied    = reader.GetInt32(0),
            Available   = reader.GetInt32(1),
            Maintenance = reader.GetInt32(2),
            Total       = reader.GetInt32(3)
        };
    }

    // ── Top 5 room types by reservation count ────────────────────────────────

    private static async Task<IReadOnlyList<RoomTypeCount>> GetTopRoomTypesAsync(
        SqlConnection conn, CancellationToken ct)
    {
        const string sql = """
            SELECT
                rt.Name                AS RoomTypeName,
                COUNT(r.ReservationId) AS ReservationCount
            FROM RoomType rt
            LEFT JOIN Room rm       ON rm.RoomTypeId = rt.RoomTypeId AND rm.IsActive = 1
            LEFT JOIN Reservation r ON r.RoomId      = rm.RoomId     AND r.IsActive  = 1
            WHERE rt.IsActive = 1
            GROUP BY rt.RoomTypeId, rt.Name
            ORDER BY ReservationCount DESC, rt.Name ASC
            """;

        await using var cmd    = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var results = new List<RoomTypeCount>();
        while (await reader.ReadAsync(ct))
            results.Add(new RoomTypeCount { RoomTypeName = reader.GetString(0), ReservationCount = reader.GetInt32(1) });

        return results.AsReadOnly();
    }
}
