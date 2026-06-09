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
[Route("api/admin/room-types")]
public class RoomTypesController : ControllerBase
{
    private readonly IAdminAuditLogService _adminAuditLogService;
    private readonly IRoomTypeRepository _roomTypeRepository;

    public RoomTypesController(IRoomTypeRepository roomTypeRepository, IAdminAuditLogService adminAuditLogService)
    {
        _roomTypeRepository = roomTypeRepository;
        _adminAuditLogService = adminAuditLogService;
    }

    // GET api/admin/room-types
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomType>>> GetAll(CancellationToken cancellationToken)
    {
        var roomTypes = await _roomTypeRepository.GetAllAsync(cancellationToken);
        return Ok(roomTypes);
    }

    // GET api/admin/room-types/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomType>> GetById(int id, CancellationToken cancellationToken)
    {
        var roomType = await _roomTypeRepository.GetByIdAsync(id, cancellationToken);
        return roomType is null
            ? NotFound(new { message = "Tipo de habitación no encontrado." })
            : Ok(roomType);
    }

    // POST api/admin/room-types
    [HttpPost]
    public async Task<ActionResult<RoomType>> Create(
        [FromBody] RoomTypeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _roomTypeRepository.CreateAsync(
                new RoomType { Name = request.Name, BasePrice = request.BasePrice },
                cancellationToken);

            await _adminAuditLogService.RecordForCurrentUserAsync(
                this,
                "Crear tipo de habitacion",
                AdminAuditDescriptionBuilder.Created(
                    "Tipo de habitacion",
                    $"{created.Name} (ID {created.RoomTypeId})",
                    [AdminAuditDescriptionBuilder.Value("tarifa base", created.BasePrice)]),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.RoomTypeId }, created);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // PUT api/admin/room-types/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomType>> Update(
        int id, [FromBody] RoomTypeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var current = await _roomTypeRepository.GetByIdAsync(id, cancellationToken);
            var updated = await _roomTypeRepository.UpdateAsync(
                new RoomType { RoomTypeId = id, Name = request.Name, BasePrice = request.BasePrice },
                cancellationToken);

            if (current is not null && updated is not null)
            {
                await _adminAuditLogService.RecordForCurrentUserAsync(
                    this,
                    "Actualizar tipo de habitacion",
                    AdminAuditDescriptionBuilder.Updated(
                        "Tipo de habitacion",
                        $"{current.Name} (ID {id})",
                        current,
                        updated),
                    cancellationToken);
            }

            return updated is null
                ? NotFound(new { message = "Tipo de habitación no encontrado." })
                : Ok(updated);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // DELETE api/admin/room-types/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var current = await _roomTypeRepository.GetByIdAsync(id, cancellationToken);
        var deleted = await _roomTypeRepository.DeleteAsync(id, cancellationToken);
        if (deleted && current is not null)
        {
            await _adminAuditLogService.RecordForCurrentUserAsync(
                this,
                "Eliminar tipo de habitacion",
                AdminAuditDescriptionBuilder.Deleted(
                    "Tipo de habitacion",
                    $"{current.Name} (ID {id}), tarifa base {current.BasePrice:0.##}"),
                cancellationToken);
        }

        return deleted
            ? NoContent()
            : NotFound(new { message = "Tipo de habitación no encontrado." });
    }
}

public sealed record RoomTypeRequest(string Name, decimal BasePrice);
