namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public class ReservationDetail
{
    public int ReservationId { get; set; }
    public DateTime ReservationDate { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal Discount { get; set; }
    public decimal SeasonAmount { get; set; }
    public int ReservationStatusId { get; set; }
    public string ReservationStatusName { get; set; } = string.Empty;
}
