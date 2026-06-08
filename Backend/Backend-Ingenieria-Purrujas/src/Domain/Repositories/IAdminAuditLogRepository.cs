using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IAdminAuditLogRepository
{
    Task<AdminAuditLog> CreateAsync(
        int adminUserId,
        string action,
        string description,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAuditLog>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default);
}
