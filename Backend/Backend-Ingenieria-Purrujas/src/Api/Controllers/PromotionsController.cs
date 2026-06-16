using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromotionsController(IPromotionRepository promotionRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Promotion>>> GetAll()
        => Ok(await promotionRepository.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Promotion>> GetById(int id)
    {
        var p = await promotionRepository.GetByIdAsync(id);
        return p is null ? NotFound() : Ok(p);
    }

}
