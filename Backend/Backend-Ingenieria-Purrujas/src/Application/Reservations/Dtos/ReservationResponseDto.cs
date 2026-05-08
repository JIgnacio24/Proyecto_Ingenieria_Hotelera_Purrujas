namespace Backend_Ingenieria_Purrujas.Application.Reservations.Dtos;

public record ReservationResponseDto
{
    public int ReservationId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public string RoomTypeName { get; init; } = string.Empty;
    public string CustomerFullName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int NightsTotal { get; init; }
    public int NightsHigh { get; init; }
    public int NightsLow { get; init; }
    public decimal TotalUsd { get; init; }
    public decimal TotalCrc { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
