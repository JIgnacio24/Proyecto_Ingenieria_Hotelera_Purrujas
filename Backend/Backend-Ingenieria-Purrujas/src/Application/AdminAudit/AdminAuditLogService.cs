using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;

namespace Backend_Ingenieria_Purrujas.Application.AdminAudit;

public class AdminAuditLogService : IAdminAuditLogService
{
    private const int DefaultTake = 100;
    private const int MaxTake = 500;
    private readonly IAdminAuditLogRepository _adminAuditLogRepository;

    public AdminAuditLogService(IAdminAuditLogRepository adminAuditLogRepository)
    {
        _adminAuditLogRepository = adminAuditLogRepository;
    }

    public async Task<AdminAuditLogDto> RecordAsync(
        int adminUserId,
        string action,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (adminUserId <= 0)
        {
            throw new ArgumentException("El usuario administrador es obligatorio.");
        }

        var normalizedAction = NormalizeRequired(action, "La accion de bitacora es obligatoria.", 150);
        var normalizedDescription = NormalizeRequired(description, "La descripcion de bitacora es obligatoria.", 600);

        var log = await _adminAuditLogRepository.CreateAsync(
            adminUserId,
            normalizedAction,
            normalizedDescription,
            cancellationToken);

        return MapToDto(log);
    }

    public async Task<IReadOnlyList<AdminAuditLogDto>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var safeTake = take <= 0 ? DefaultTake : Math.Min(take, MaxTake);
        var logs = await _adminAuditLogRepository.GetRecentAsync(safeTake, cancellationToken);
        return logs.Select(MapToDto).ToList();
    }

    private static string NormalizeRequired(string value, string errorMessage, int maxLength)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException(errorMessage);
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private static AdminAuditLogDto MapToDto(AdminAuditLog log)
    {
        return new AdminAuditLogDto(
            log.AdminAuditLogId,
            log.AdminUserId,
            log.FullName,
            log.Username,
            log.OccurredAt,
            log.Action,
            log.Description);
    }
}
