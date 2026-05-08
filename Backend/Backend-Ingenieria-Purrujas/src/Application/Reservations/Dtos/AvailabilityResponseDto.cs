namespace Backend_Ingenieria_Purrujas.Application.Reservations.Dtos;

public record AvailabilityResponseDto
{
    public bool IsAvailable { get; init; }
    public int AvailableRooms { get; init; }
    public string RoomTypeName { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}
