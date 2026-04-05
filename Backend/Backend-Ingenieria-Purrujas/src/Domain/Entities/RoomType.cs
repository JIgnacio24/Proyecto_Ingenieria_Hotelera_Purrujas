namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public class RoomType
{
    public int RoomTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
}
