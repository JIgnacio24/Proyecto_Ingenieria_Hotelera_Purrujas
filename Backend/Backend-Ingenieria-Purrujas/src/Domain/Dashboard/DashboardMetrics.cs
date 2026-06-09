namespace Backend_Ingenieria_Purrujas.Domain.Dashboard;

public record DashboardMetrics
{
    public KpiSummary Kpi { get; init; } = new();
    public IReadOnlyList<MonthlyDataPoint> ReservationsByMonth { get; init; } = [];
    public IReadOnlyList<MonthlyDataPoint> RevenueByMonth { get; init; } = [];
    public IReadOnlyList<StatusCount> ReservationsByStatus { get; init; } = [];
    public RoomStatusSummary RoomStatus { get; init; } = new();
    public IReadOnlyList<RoomTypeCount> TopRoomTypes { get; init; } = [];
}

public record KpiSummary
{
    public int ReservationsThisMonth { get; init; }
    public int ConfirmedThisMonth { get; init; }
    public decimal RevenueThisMonthUsd { get; init; }
    public int TotalRooms { get; init; }
    public int OccupiedRooms { get; init; }
    public int AvailableRooms { get; init; }
}

public record MonthlyDataPoint
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int Count { get; init; }
    public decimal Amount { get; init; }
}

public record StatusCount
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record RoomStatusSummary
{
    public int Available { get; init; }
    public int Occupied { get; init; }
    public int Maintenance { get; init; }
    public int Total { get; init; }
}

public record RoomTypeCount
{
    public string RoomTypeName { get; init; } = string.Empty;
    public int ReservationCount { get; init; }
}
