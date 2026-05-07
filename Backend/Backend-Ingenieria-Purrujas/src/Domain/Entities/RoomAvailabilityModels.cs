namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public sealed class RoomTypeOption
{
    public int RoomTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class RoomStatusTodayItem
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string? CurrentGuest { get; set; }
}

public sealed class RoomAvailabilitySummary
{
    public DateOnly Date { get; set; }
    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int OutOfServiceRooms { get; set; }
    public IReadOnlyList<RoomStatusTodayItem> Rooms { get; set; } = [];
    public IReadOnlyList<RoomTypeOption> RoomTypes { get; set; } = [];
}

public sealed class RoomAvailabilityItem
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string OperationalStatus { get; set; } = string.Empty;
    public decimal BasePricePerNight { get; set; }
    public int NightsTotal { get; set; }
    public int HighSeasonNights { get; set; }
    public int LowSeasonNights { get; set; }
    public decimal HighestSeasonMultiplier { get; set; }
    public decimal TotalUsd { get; set; }
}

public sealed class RoomAvailabilitySearchResult
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = "Todos los tipos";
    public int AvailableRooms { get; set; }
    public IReadOnlyList<RoomAvailabilityItem> Rooms { get; set; } = [];
}
