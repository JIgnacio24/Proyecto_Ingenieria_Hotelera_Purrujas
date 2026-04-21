using System.ComponentModel.DataAnnotations;

namespace Backend_Ingenieria_Purrujas.Application.Auth;

public sealed class LoginRequestDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]{4,50}$", ErrorMessage = "El nombre de usuario no tiene un formato valido.")]
    [StringLength(100)]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [StringLength(255, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}
