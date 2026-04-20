using System.ComponentModel.DataAnnotations;

namespace Backend_Ingenieria_Purrujas.Application.Auth;

public sealed class RegisterAdminUserRequestDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(255)]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(100, MinimumLength = 4)]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es valido.")]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [StringLength(255, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [StringLength(50)]
    public string Role { get; init; } = "Administrador";
}
