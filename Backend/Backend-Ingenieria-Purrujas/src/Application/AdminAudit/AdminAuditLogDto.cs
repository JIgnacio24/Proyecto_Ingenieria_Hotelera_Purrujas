namespace Backend_Ingenieria_Purrujas.Application.AdminAudit;

public sealed record AdminAuditLogDto(
    int AdminAuditLogId,
    int AdminUserId,
    string FullName,
    string Username,
    DateTime OccurredAt,
    string Action,
    string Description);
