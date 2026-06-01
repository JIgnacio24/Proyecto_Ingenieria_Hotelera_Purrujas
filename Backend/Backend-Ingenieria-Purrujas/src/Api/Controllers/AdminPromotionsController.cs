using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/promotions")]
public class AdminPromotionsController : ControllerBase
{
    private readonly IPromotionRepository _promotionRepository;

    public AdminPromotionsController(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    // GET api/admin/promotions
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Promotion>>> GetAll(CancellationToken ct)
    {
        var promotions = await _promotionRepository.GetAllAsync(ct);
        return Ok(promotions);
    }

    // GET api/admin/promotions/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Promotion>> GetById(int id, CancellationToken ct)
    {
        var promotion = await _promotionRepository.GetByIdAsync(id, ct);
        return promotion is null
            ? NotFound(new { message = "Oferta no encontrada." })
            : Ok(promotion);
    }

    // POST api/admin/promotions
    [HttpPost]
    public async Task<ActionResult<Promotion>> Create(
        [FromBody] PromotionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var promotion = new Promotion
            {
                Name = request.Name,
                Discount = request.Discount,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                RoomTypeId = request.RoomTypeId
            };

            var created = await _promotionRepository.CreateAsync(promotion, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.PromotionId }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT api/admin/promotions/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Promotion>> Update(
        int id, [FromBody] PromotionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var promotion = new Promotion
            {
                PromotionId = id,
                Name = request.Name,
                Discount = request.Discount,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                RoomTypeId = request.RoomTypeId
            };

            var updated = await _promotionRepository.UpdateAsync(promotion, ct);
            return updated is null
                ? NotFound(new { message = "Oferta no encontrada." })
                : Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE api/admin/promotions/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _promotionRepository.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed record PromotionRequest(
    string Name,
    int Discount,
    DateOnly StartDate,
    DateOnly EndDate,
    int RoomTypeId);
