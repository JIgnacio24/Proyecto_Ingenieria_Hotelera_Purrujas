using Backend_Ingenieria_Purrujas.Domain.Dashboard;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class DashboardController(
    IDashboardRepository dashboardRepository,
    ILogger<DashboardController> logger) : ControllerBase
{
    [HttpGet("metrics")]
    public async Task<ActionResult<DashboardMetrics>> GetMetrics(CancellationToken cancellationToken)
    {
        try
        {
            var metrics = await dashboardRepository.GetMetricsAsync(cancellationToken);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener las métricas del dashboard");
            return StatusCode(500, new { message = $"Error interno: {ex.GetType().Name} — {ex.Message}" });
        }
    }
}
