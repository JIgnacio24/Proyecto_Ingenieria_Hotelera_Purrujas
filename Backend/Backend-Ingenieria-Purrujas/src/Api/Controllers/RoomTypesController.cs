using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

<<<<<<< Updated upstream
=======
/// <summary>
/// Expone los tipos de habitación con descripción, capacidad e imágenes
/// para el módulo administrativo.
/// </summary>
>>>>>>> Stashed changes
[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/room-types")]
public class RoomTypesController : ControllerBase
{
    private readonly IRoomTypeRepository _roomTypeRepository;

    public RoomTypesController(IRoomTypeRepository roomTypeRepository)
    {
        _roomTypeRepository = roomTypeRepository;
    }

<<<<<<< Updated upstream
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomType>>> GetAll(CancellationToken cancellationToken)
=======
    /// <summary>
    /// Retorna todos los tipos de habitación con su descripción completa,
    /// capacidad, precio base, estado y lista de imágenes.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomType>>> GetAll(
        CancellationToken cancellationToken)
>>>>>>> Stashed changes
    {
        var roomTypes = await _roomTypeRepository.GetAllAsync(cancellationToken);
        return Ok(roomTypes);
    }
<<<<<<< Updated upstream

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomType>> GetById(int id, CancellationToken cancellationToken)
    {
        var roomType = await _roomTypeRepository.GetByIdAsync(id, cancellationToken);
        return roomType is null ? NotFound(new { message = "Tipo de habitación no encontrado." }) : Ok(roomType);
    }

    [HttpPost]
    public async Task<ActionResult<RoomType>> Create(
        [FromBody] RoomTypeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _roomTypeRepository.CreateAsync(
                new RoomType
                {
                    Name = request.Name,
                    BasePrice = request.BasePrice
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.RoomTypeId }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomType>> Update(
        int id,
        [FromBody] RoomTypeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _roomTypeRepository.UpdateAsync(
                new RoomType
                {
                    RoomTypeId = id,
                    Name = request.Name,
                    BasePrice = request.BasePrice
                },
                cancellationToken);

            return updated is null
                ? NotFound(new { message = "Tipo de habitación no encontrado." })
                : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _roomTypeRepository.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { message = "Tipo de habitación no encontrado." });
    }
}

public sealed record RoomTypeRequest(string Name, decimal BasePrice);
=======
}
>>>>>>> Stashed changes
