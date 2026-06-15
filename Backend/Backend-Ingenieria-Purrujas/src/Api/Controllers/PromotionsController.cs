using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromotionsController(IPromotionRepository promotionRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PublicPromotion>>> GetAll(CancellationToken cancellationToken = default)
    {
        var promotions = await promotionRepository.GetAllAsync(cancellationToken);
        return Ok(promotions.Select(MapPromotion));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PublicPromotion>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await promotionRepository.GetByIdAsync(id, cancellationToken);
        return promotion is null ? NotFound() : Ok(MapPromotion(promotion));
    }

    private static PublicPromotion MapPromotion(Promotion promotion) => new(
        promotion.PromotionId,
        promotion.Name,
        "http://localhost:4200/reservar",
        promotion.Discount,
        promotion.StartDate,
        promotion.EndDate,
        promotion.RoomTypeId);
}

public sealed record PublicPromotion(
    int PromotionId,
    string Name,
    string Link,
    int Discount,
    DateOnly StartDate,
    DateOnly EndDate,
    int RoomTypeId);
