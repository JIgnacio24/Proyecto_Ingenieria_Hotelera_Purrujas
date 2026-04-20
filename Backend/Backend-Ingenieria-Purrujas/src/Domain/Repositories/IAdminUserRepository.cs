using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IAdminUserRepository
{
    Task<AdminUser> RegisterAsync(
        string fullName,
        string username,
        string email,
        string password,
        string role,
        CancellationToken cancellationToken = default);

    Task<AdminUser?> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    Task<AdminUser?> GetByIdAsync(int adminUserId, CancellationToken cancellationToken = default);
}
