using System.ComponentModel.DataAnnotations;

namespace Backend_Ingenieria_Purrujas.Application.Reservations.Dtos;

public record CreateReservationRequestDto
{
    [Required]
    public string RoomTypeKey { get; init; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; init; }

    [Required]
    public DateOnly EndDate { get; init; }

    [Required]
    public string Currency { get; init; } = "USD";

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Phone]
    public string? Phone { get; init; }

    [Required]
    [RegularExpression(@"^\d{13,19}$", ErrorMessage = "El número de tarjeta debe tener entre 13 y 19 dígitos.")]
    public string CreditCard { get; init; } = string.Empty;
}
