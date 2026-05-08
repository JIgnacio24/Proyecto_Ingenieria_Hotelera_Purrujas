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
    private readonly IRoomAvailabilityRepository _roomAvailabilityRepository;

    public RoomAvailabilityController(IRoomAvailabilityRepository roomAvailabilityRepository)
    {
        _roomAvailabilityRepository = roomAvailabilityRepository;
    }

    [HttpGet("today")]
    public async Task<ActionResult<RoomAvailabilitySummary>> Today(CancellationToken cancellationToken)
    {
        // La consulta usa la fecha local del servidor para reflejar el estado operativo del dia actual.
        var today = DateOnly.FromDateTime(DateTime.Now);
        var summary = await _roomAvailabilityRepository.GetTodayAsync(today, cancellationToken);
        return Ok(summary);
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
