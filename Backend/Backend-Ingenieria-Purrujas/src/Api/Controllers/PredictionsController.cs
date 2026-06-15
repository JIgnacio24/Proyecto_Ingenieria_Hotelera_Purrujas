using Backend_Ingenieria_Purrujas.Application.Occupancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/predictions")]
public class PredictionsController : ControllerBase
{
    private readonly IOccupancyPredictionService _predictionService;

    public PredictionsController(IOccupancyPredictionService predictionService)
    {
        _predictionService = predictionService;
    }

    // GET api/predictions/occupancy
    [HttpGet("occupancy")]
    public async Task<IActionResult> GetOccupancyPredictions()
    {
        try
        {
            var predictions = await _predictionService.PredictAsync();
            return Ok(predictions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al generar predicciones de ocupación.", detail = ex.Message });
        }
    }

    // GET api/predictions/history
    [HttpGet("history")]
    public async Task<IActionResult> GetOccupancyHistory()
    {
        try
        {
            var history = await _predictionService.GetHistoryAsync();
            return Ok(history);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener historial de ocupación.", detail = ex.Message });
        }
    }
}
