using Backend_Ingenieria_Purrujas.Application.Reservations;
using Backend_Ingenieria_Purrujas.Application.Reservations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationController(IReservationService reservationService) : ControllerBase
{
    [HttpGet("availability")]
    public async Task<ActionResult<AvailabilityResponseDto>> CheckAvailability(
        [FromQuery] string roomTypeKey,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roomTypeKey))
            return BadRequest(new { message = "El tipo de habitación es requerido." });

        if (startDate >= endDate)
            return BadRequest(new { message = "La fecha de salida debe ser posterior a la de entrada." });

        try
        {
            var result = await reservationService.CheckAvailabilityAsync(roomTypeKey, startDate, endDate, cancellationToken);
            return Ok(result);
        }
        catch (SqlException)
        {
            return StatusCode(503, new { message = "El servicio de disponibilidad no está disponible. Verifique que el DB patch fue aplicado." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error interno al verificar disponibilidad." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ReservationResponseDto>> Create(
        [FromBody] CreateReservationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var result = await reservationService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.ReservationId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ReservationResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var results = await reservationService.GetAllAsync(cancellationToken);
        return Ok(results);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await reservationService.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            await reservationService.UpdateStatusAsync(id, request.Status, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
