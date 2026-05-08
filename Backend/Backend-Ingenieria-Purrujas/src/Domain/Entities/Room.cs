namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public class Room
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int RoomTypeId { get; set; }
    public int RoomStatusId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
}
