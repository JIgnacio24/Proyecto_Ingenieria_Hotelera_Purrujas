namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public class Promotion
{
    public int PromotionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Discount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
