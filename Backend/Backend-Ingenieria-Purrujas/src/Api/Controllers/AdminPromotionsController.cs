using Backend_Ingenieria_Purrujas.Api.Extensions;
using Backend_Ingenieria_Purrujas.Api.Services;
using Backend_Ingenieria_Purrujas.Application.AdminAudit;
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
    private readonly IAdminAuditLogService _adminAuditLogService;
    private readonly IPromotionRepository _promotionRepository;

    public AdminPromotionsController(IPromotionRepository promotionRepository, IAdminAuditLogService adminAuditLogService)
    {
        _promotionRepository = promotionRepository;
        _adminAuditLogService = adminAuditLogService;
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
            await _adminAuditLogService.RecordForCurrentUserAsync(
                this,
                "Crear oferta",
                AdminAuditDescriptionBuilder.Created(
                    "Oferta",
                    $"{created.Name} (ID {created.PromotionId})",
                    [
                        AdminAuditDescriptionBuilder.Value("descuento", $"{created.Discount}%"),
                        AdminAuditDescriptionBuilder.Value("tipo de habitacion", created.RoomTypeName),
                        AdminAuditDescriptionBuilder.Value("fecha inicial", created.StartDate),
                        AdminAuditDescriptionBuilder.Value("fecha final", created.EndDate)
                    ]),
                ct);

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
            var current = await _promotionRepository.GetByIdAsync(id, ct);
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
            if (current is not null && updated is not null)
            {
                await _adminAuditLogService.RecordForCurrentUserAsync(
                    this,
                    "Actualizar oferta",
                    AdminAuditDescriptionBuilder.Updated(
                        "Oferta",
                        $"{current.Name} (ID {id})",
                        current,
                        updated),
                    ct);
            }

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
            var current = await _promotionRepository.GetByIdAsync(id, ct);
            var deleted = await _promotionRepository.DeleteAsync(id, ct);
            if (deleted && current is not null)
            {
                await _adminAuditLogService.RecordForCurrentUserAsync(
                    this,
                    "Eliminar oferta",
                    AdminAuditDescriptionBuilder.Deleted(
                        "Oferta",
                        $"{current.Name} (ID {id}), descuento {current.Discount}%, tipo {current.RoomTypeName}"),
                    ct);
            }

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
