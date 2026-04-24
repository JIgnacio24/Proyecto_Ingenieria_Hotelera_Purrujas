namespace Backend_Ingenieria_Purrujas.Application.Auth;

public sealed record AuthResponseDto(
    string Token,
    DateTime ExpiresAt,
    AdminUserDto User
);
