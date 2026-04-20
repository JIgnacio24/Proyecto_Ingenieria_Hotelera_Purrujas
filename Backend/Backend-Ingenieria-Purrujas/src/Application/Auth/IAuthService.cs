namespace Backend_Ingenieria_Purrujas.Application.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterAdminUserRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AdminUserDto?> GetProfileAsync(int adminUserId, CancellationToken cancellationToken = default);
}
