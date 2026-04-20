using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend_Ingenieria_Purrujas.Application.Auth;

public class AuthService : IAuthService
{
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

        var user = await _adminUserRepository.RegisterAsync(
            request.FullName.Trim(),
            request.Username.Trim(),
            request.Email.Trim(),
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
            throw new UnauthorizedAccessException("Nombre de usuario o contrasena incorrectos.");
        }

        return BuildAuthResponse(user);
    }

    public async Task<AdminUserDto?> GetProfileAsync(int adminUserId, CancellationToken cancellationToken = default)
    {
        var user = await _adminUserRepository.GetByIdAsync(adminUserId, cancellationToken);
        return user is null ? null : MapToDto(user);
    }

    private static void ValidateRegisterRequest(RegisterAdminUserRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new ArgumentException("El nombre completo es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentException("El nombre de usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("El correo es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new ArgumentException("La contrasena debe tener al menos 8 caracteres.");
        }
    }

    private static void ValidateLoginRequest(LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentException("El nombre de usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("La contrasena es obligatoria.");
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
            throw new InvalidOperationException("No se configuro Jwt:Key en el backend.");
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
        return string.IsNullOrWhiteSpace(role) ? "Administrador" : role.Trim();
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
