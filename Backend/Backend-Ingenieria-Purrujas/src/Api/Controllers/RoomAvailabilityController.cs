using System.Security.Claims;
using Backend_Ingenieria_Purrujas.Api.Services;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/room-availability")]
public class RoomAvailabilityController : ControllerBase
{
    private const string SystemName = "Hotel Las Purrujas";
    private readonly IRoomAvailabilityRepository _roomAvailabilityRepository;
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly RoomAvailabilityPdfService _roomAvailabilityPdfService;

    public RoomAvailabilityController(
        IRoomAvailabilityRepository roomAvailabilityRepository,
        IAdminUserRepository adminUserRepository,
        RoomAvailabilityPdfService roomAvailabilityPdfService)
    {
        _roomAvailabilityRepository = roomAvailabilityRepository;
        _adminUserRepository = adminUserRepository;
        _roomAvailabilityPdfService = roomAvailabilityPdfService;
    }

    [HttpGet("today")]
    public async Task<ActionResult<RoomAvailabilitySummary>> Today(CancellationToken cancellationToken)
    {
        // La consulta usa la fecha local del servidor para reflejar el estado operativo del día actual.
        var today = DateOnly.FromDateTime(DateTime.Now);
        var summary = await _roomAvailabilityRepository.GetTodayAsync(today, cancellationToken);
        return Ok(summary);
    }

    [HttpGet("today/report")]
    public async Task<IActionResult> TodayReport(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminUserId))
        {
            return Unauthorized(new { message = "No fue posible identificar al usuario autenticado." });
        }

        var adminUser = await _adminUserRepository.GetByIdAsync(adminUserId, cancellationToken);
        if (adminUser is null || !adminUser.IsActive)
        {
            return Forbid();
        }

        try
        {
            var generatedAt = DateTime.Now;
            var today = DateOnly.FromDateTime(generatedAt);
            var summary = await _roomAvailabilityRepository.GetTodayAsync(today, cancellationToken);
            var report = _roomAvailabilityPdfService.Generate(summary, adminUser, generatedAt, SystemName);

            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            Response.Headers.ContentDisposition = $"inline; filename=\"{report.FileName}\"";

            return File(report.Content, "application/pdf");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "No fue posible generar el reporte PDF del estado de habitaciones."
            });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<RoomAvailabilitySearchResult>> Search(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int? roomTypeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roomAvailabilityRepository.SearchAsync(
                startDate,
                endDate,
                roomTypeId,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
