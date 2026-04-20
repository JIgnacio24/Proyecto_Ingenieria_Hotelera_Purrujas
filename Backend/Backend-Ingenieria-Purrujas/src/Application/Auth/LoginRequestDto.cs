using System.ComponentModel.DataAnnotations;

namespace Backend_Ingenieria_Purrujas.Application.Auth;

public sealed class LoginRequestDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(100)]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [StringLength(255, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}
