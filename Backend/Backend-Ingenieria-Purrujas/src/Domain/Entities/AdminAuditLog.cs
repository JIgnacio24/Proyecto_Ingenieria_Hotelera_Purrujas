namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public class AdminAuditLog
{
    public int AdminAuditLogId { get; set; }
    public int AdminUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
