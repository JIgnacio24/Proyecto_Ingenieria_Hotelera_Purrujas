using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace Backend_Ingenieria_Purrujas.Application.Auth;

public class AuthService : IAuthService
{
    private const string AdministratorRole = "Administrador";
    private static readonly EmailAddressAttribute EmailValidator = new();
    private static readonly Regex FullNameRegex = new(
        @"^[\p{L}][\p{L}\s'.-]{5,254}$",
        RegexOptions.Compiled);
    private static readonly Regex UsernameRegex = new(
        @"^[a-zA-Z0-9._-]{4,50}$",
        RegexOptions.Compiled);
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IAdminUserRepository adminUserRepository, IConfiguration configuration)
    {
        _adminUserRepository = adminUserRepository;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterAdminUserRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateRegisterRequest(request);
        var fullName = request.FullName.Trim();
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        var user = await _adminUserRepository.RegisterAsync(
            fullName,
            username,
            email,
            request.Password,
            NormalizeRole(request.Role),
            cancellationToken);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateLoginRequest(request);

        var user = await _adminUserRepository.LoginAsync(
            request.Username.Trim(),
            request.Password,
            cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Nombre de usuario o contraseña incorrectos.");
        }

        if (!IsAdministratorRole(user.Role))
        {
            throw new UnauthorizedAccessException("Solo un administrador autenticado puede acceder al módulo administrativo.");
        }

        return BuildAuthResponse(user);
    }

    public async Task<AdminUserDto?> GetProfileAsync(int adminUserId, CancellationToken cancellationToken = default)
    {
        var user = await _adminUserRepository.GetByIdAsync(adminUserId, cancellationToken);
        return user is null || !IsAdministratorRole(user.Role) ? null : MapToDto(user);
    }

    private static void ValidateRegisterRequest(RegisterAdminUserRequestDto request)
    {
        var fullName = request.FullName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("El nombre completo es obligatorio.");
        }

        if (!FullNameRegex.IsMatch(fullName))
        {
            throw new ArgumentException("El nombre completo contiene caracteres no válidos.");
        }

        var username = request.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("El nombre de usuario es obligatorio.");
        }

        if (!UsernameRegex.IsMatch(username))
        {
            throw new ArgumentException("El nombre de usuario solo puede incluir letras, números, punto, guion y guion bajo.");
        }

        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("El correo es obligatorio.");
        }

        if (!EmailValidator.IsValid(email))
        {
            throw new ArgumentException("El correo no tiene un formato válido.");
        }

        ValidatePassword(request.Password, requireComplexity: true);
    }

    private static void ValidateLoginRequest(LoginRequestDto request)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("El nombre de usuario es obligatorio.");
        }

        if (!UsernameRegex.IsMatch(username))
        {
            throw new ArgumentException("El nombre de usuario no tiene un formato válido.");
        }

        ValidatePassword(request.Password, requireComplexity: false);
    }

    private static void ValidatePassword(string? password, bool requireComplexity)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 255)
        {
            throw new ArgumentException("La contraseña debe tener entre 8 y 255 caracteres.");
        }

        if (!requireComplexity)
        {
            return;
        }

        var hasUppercase = password.Any(char.IsUpper);
        var hasLowercase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialCharacter = password.Any(ch => !char.IsLetterOrDigit(ch));

        if (!hasUppercase || !hasLowercase || !hasDigit || !hasSpecialCharacter)
        {
            throw new ArgumentException(
                "La contraseña debe incluir mayúscula, minúscula, número y carácter especial.");
        }
    }

    private AuthResponseDto BuildAuthResponse(AdminUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes());
        var token = GenerateToken(user, expiresAt);

        return new AuthResponseDto(token, expiresAt, MapToDto(user));
    }

    private string GenerateToken(AdminUser user, DateTime expiresAt)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("No se configuró Jwt:Key en el backend.");
        }

        var issuer = _configuration["Jwt:Issuer"] ?? "Backend-Ingenieria-Purrujas";
        var audience = _configuration["Jwt:Audience"] ?? "Frontend-Ingenieria-Purrujas-Admin";

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.AdminUserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.AdminUserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetTokenExpirationMinutes()
    {
        return int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var minutes) && minutes > 0
            ? minutes
            : 480;
    }

    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role) ||
            string.Equals(role.Trim(), AdministratorRole, StringComparison.OrdinalIgnoreCase))
        {
            return AdministratorRole;
        }

        return role.Trim();
    }

    private static bool IsAdministratorRole(string? role)
    {
        return string.Equals(role?.Trim(), AdministratorRole, StringComparison.OrdinalIgnoreCase);
    }

    private static AdminUserDto MapToDto(AdminUser user)
    {
        return new AdminUserDto(
            user.AdminUserId,
            user.FullName,
            user.Username,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt
        );
    }
}
