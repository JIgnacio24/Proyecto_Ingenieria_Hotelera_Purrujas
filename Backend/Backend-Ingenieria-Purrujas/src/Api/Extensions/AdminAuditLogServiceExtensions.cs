using Backend_Ingenieria_Purrujas.Application.AdminAudit;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend_Ingenieria_Purrujas.Api.Extensions;

public static class AdminAuditLogServiceExtensions
{
    public static async Task RecordForCurrentUserAsync(
        this IAdminAuditLogService adminAuditLogService,
        ControllerBase controller,
        string action,
        string description,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var adminUserId))
        {
            return;
        }

        await adminAuditLogService.RecordAsync(adminUserId, action, description, cancellationToken);
    }
}
