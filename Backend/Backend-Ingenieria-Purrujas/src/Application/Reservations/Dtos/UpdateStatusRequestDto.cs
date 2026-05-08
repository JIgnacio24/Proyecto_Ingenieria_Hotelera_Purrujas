using System.ComponentModel.DataAnnotations;

namespace Backend_Ingenieria_Purrujas.Application.Reservations.Dtos;

public record UpdateStatusRequestDto
{
    [Required]
    public string Status { get; init; } = string.Empty;
}
