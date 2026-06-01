using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/seasons")]
public class SeasonsController : ControllerBase
{
    private readonly ISeasonRepository _seasonRepository;

    public SeasonsController(ISeasonRepository seasonRepository)
    {
        _seasonRepository = seasonRepository;
    }

    // GET api/admin/seasons
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Season>>> GetAll(CancellationToken ct)
    {
        var seasons = await _seasonRepository.GetAllAsync(ct);
        return Ok(seasons);
    }

    // GET api/admin/seasons/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Season>> GetById(int id, CancellationToken ct)
    {
        var season = await _seasonRepository.GetByIdAsync(id, ct);
        return season is null
            ? NotFound(new { message = "Temporada no encontrada." })
            : Ok(season);
    }

    // POST api/admin/seasons
    [HttpPost]
    public async Task<ActionResult<Season>> Create([FromBody] SeasonRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var season = new Season
            {
                Name = request.Name,
                PercentageChange = request.PercentageChange,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            var created = await _seasonRepository.CreateAsync(season, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.SeasonId }, created);
        }
        catch (Exception ex) when (ex.Message.Contains("mayor o igual"))
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT api/admin/seasons/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Season>> Update(
        int id, [FromBody] SeasonRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var season = new Season
            {
                SeasonId = id,
                Name = request.Name,
                PercentageChange = request.PercentageChange,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            var updated = await _seasonRepository.UpdateAsync(season, ct);
            return updated is null
                ? NotFound(new { message = "Temporada no encontrada." })
                : Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE api/admin/seasons/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _seasonRepository.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed record SeasonRequest(
    string Name,
    int PercentageChange,
    DateOnly StartDate,
    DateOnly EndDate);
