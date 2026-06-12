namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public class RoomType
{
    public int RoomTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public int Capacity { get; set; } = 2;
}
