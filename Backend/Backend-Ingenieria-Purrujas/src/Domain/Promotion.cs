namespace Backend_Ingenieria_Purrujas.Domain;

public class Promotion
{
    public int PromotionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public int Discount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int RoomTypeId { get; set; }
}
