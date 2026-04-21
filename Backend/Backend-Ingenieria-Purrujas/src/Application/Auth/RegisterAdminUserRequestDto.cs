using System.ComponentModel.DataAnnotations;

namespace Backend_Ingenieria_Purrujas.Application.Auth;

public sealed class RegisterAdminUserRequestDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [RegularExpression(@"^[\p{L}][\p{L}\s'.-]{5,254}$", ErrorMessage = "El nombre completo contiene caracteres no validos.")]
    [StringLength(255)]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]{4,50}$", ErrorMessage = "El nombre de usuario no tiene un formato valido.")]
    [StringLength(100, MinimumLength = 4)]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es valido.")]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,255}$",
        ErrorMessage = "La contrasena debe incluir mayuscula, minuscula, numero y caracter especial.")]
    [StringLength(255, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [StringLength(50)]
    public string Role { get; init; } = "Administrador";
}
