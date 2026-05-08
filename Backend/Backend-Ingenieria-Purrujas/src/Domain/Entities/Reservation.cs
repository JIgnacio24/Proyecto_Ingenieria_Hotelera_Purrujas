namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public class Reservation
{
    public int ReservationId { get; set; }
    public DateTime ReservationDate { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int CustomerId { get; set; }
    public int RoomId { get; set; }
    public int ReservationStatusId { get; set; }
    public bool IsActive { get; set; }
}
