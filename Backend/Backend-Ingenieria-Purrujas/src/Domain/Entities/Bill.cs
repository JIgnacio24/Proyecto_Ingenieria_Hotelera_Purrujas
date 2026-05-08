namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public class Bill
{
    public int BillId { get; set; }
    public int ReservationId { get; set; }
    public decimal BasePrice { get; set; }
    public decimal Discount { get; set; }
    public decimal SeasonAmount { get; set; }
}
