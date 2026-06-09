namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public sealed class RoomStatusOption
{
    public int RoomStatusId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAvailableForBooking { get; set; }
}
