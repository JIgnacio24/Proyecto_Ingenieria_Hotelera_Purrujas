using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdvertisingController : ControllerBase
{
    private readonly IAdvertisingRepository _advertisingRepository;

    public AdvertisingController(IAdvertisingRepository advertisingRepository)
    {
        _advertisingRepository = advertisingRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Advertising>>> GetAll(CancellationToken ct)
    {
        var items = await _advertisingRepository.GetAllAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Advertising>> GetById(int id, CancellationToken ct)
    {
        var item = await _advertisingRepository.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }
}
