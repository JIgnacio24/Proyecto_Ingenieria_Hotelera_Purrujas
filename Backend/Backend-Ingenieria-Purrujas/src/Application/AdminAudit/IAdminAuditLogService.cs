namespace Backend_Ingenieria_Purrujas.Application.AdminAudit;

public interface IAdminAuditLogService
{
    Task<AdminAuditLogDto> RecordAsync(
        int adminUserId,
        string action,
        string description,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAuditLogDto>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default);
}
