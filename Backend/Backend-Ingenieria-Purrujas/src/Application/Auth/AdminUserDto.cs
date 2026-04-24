namespace Backend_Ingenieria_Purrujas.Application.Auth;

public sealed record AdminUserDto(
    int AdminUserId,
    string FullName,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
