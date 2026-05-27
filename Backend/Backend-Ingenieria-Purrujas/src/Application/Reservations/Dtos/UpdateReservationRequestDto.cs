using System.ComponentModel.DataAnnotations;

namespace Backend_Ingenieria_Purrujas.Application.Reservations.Dtos;

public sealed class UpdateReservationRequestDto
{
    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La habitación es requerida.")]
    public int RoomId { get; set; }
}
