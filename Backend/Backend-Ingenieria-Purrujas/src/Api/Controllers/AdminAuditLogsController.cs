using Backend_Ingenieria_Purrujas.Application.AdminAudit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/audit-logs")]
public class AdminAuditLogsController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, string> AdminViewNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["dashboard"] = "Panel administrativo",
            ["home-editor"] = "Editor de inicio",
            ["about-us-editor"] = "Editor de Sobre nosotros",
            ["reservations"] = "Reservaciones",
            ["room-types"] = "Tipos de habitacion",
            ["rooms"] = "Habitaciones",
            ["seasons"] = "Temporadas",
            ["promotions"] = "Ofertas especiales",
            ["audit-logs"] = "Bitacoras de uso"
        };

    private readonly IAdminAuditLogService _adminAuditLogService;

    public AdminAuditLogsController(IAdminAuditLogService adminAuditLogService)
    {
        _adminAuditLogService = adminAuditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminAuditLogDto>>> GetRecent(
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var logs = await _adminAuditLogService.GetRecentAsync(take, cancellationToken);
        return Ok(logs);
    }

    [HttpPost("view-entry")]
    public async Task<ActionResult<AdminAuditLogDto>> RecordViewEntry(
        [FromBody] RecordAdminViewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var viewKey = request.ViewKey?.Trim() ?? string.Empty;
        if (!AdminViewNames.TryGetValue(viewKey, out var viewName))
        {
            return BadRequest(new { message = "La vista administrativa no es valida." });
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var adminUserId))
        {
            return Unauthorized(new { message = "La sesion no es valida." });
        }

        var log = await _adminAuditLogService.RecordAsync(
            adminUserId,
            $"Ingresar a vista: {viewName}",
            $"Ingreso a la vista {viewName} del modulo administrativo.",
            cancellationToken);

        return Ok(log);
    }
}
