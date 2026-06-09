using Backend_Ingenieria_Purrujas.Api.Extensions;
using Backend_Ingenieria_Purrujas.Api.Services;
using Backend_Ingenieria_Purrujas.Application.AdminAudit;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IAdminAuditLogService _adminAuditLogService;
    private readonly IRoomRepository _roomRepository;

    public RoomsController(IRoomRepository roomRepository, IAdminAuditLogService adminAuditLogService)
    {
        _roomRepository = roomRepository;
        _adminAuditLogService = adminAuditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Room>>> GetAll(CancellationToken cancellationToken)
    {
        var rooms = await _roomRepository.GetAllAsync(cancellationToken);
        return Ok(rooms);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Room>> GetById(int id, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdAsync(id, cancellationToken);
        return room is null
            ? NotFound(new { message = "Habitación no encontrada." })
            : Ok(room);
    }

    [HttpGet("statuses")]
    public async Task<ActionResult<IReadOnlyList<RoomStatusOption>>> GetStatuses(CancellationToken cancellationToken)
    {
        var statuses = await _roomRepository.GetRoomStatusesAsync(cancellationToken);
        return Ok(statuses);
    }

    [HttpPost]
    public async Task<ActionResult<Room>> Create([FromBody] RoomRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _roomRepository.CreateAsync(
                new Room
                {
                    RoomNumber = request.RoomNumber,
                    RoomTypeId = request.RoomTypeId,
                    RoomStatusId = request.RoomStatusId
                },
                cancellationToken);

            await _adminAuditLogService.RecordForCurrentUserAsync(
                this,
                "Crear habitacion",
                AdminAuditDescriptionBuilder.Created(
                    "Habitacion",
                    $"Habitacion {created.RoomNumber} (ID {created.RoomId})",
                    [
                        AdminAuditDescriptionBuilder.Value("tipo de habitacion", created.RoomTypeName),
                        AdminAuditDescriptionBuilder.Value("estado", created.RoomStatusName)
                    ]),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.RoomId }, created);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (SqlException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Room>> Update(int id, [FromBody] RoomRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var current = await _roomRepository.GetByIdAsync(id, cancellationToken);
            var updated = await _roomRepository.UpdateAsync(
                new Room
                {
                    RoomId = id,
                    RoomNumber = request.RoomNumber,
                    RoomTypeId = request.RoomTypeId,
                    RoomStatusId = request.RoomStatusId
                },
                cancellationToken);

            if (current is not null && updated is not null)
            {
                await _adminAuditLogService.RecordForCurrentUserAsync(
                    this,
                    "Actualizar habitacion",
                    AdminAuditDescriptionBuilder.Updated(
                        "Habitacion",
                        $"Habitacion {current.RoomNumber} (ID {id})",
                        current,
                        updated),
                    cancellationToken);
            }

            return updated is null
                ? NotFound(new { message = "Habitación no encontrada." })
                : Ok(updated);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (SqlException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var current = await _roomRepository.GetByIdAsync(id, cancellationToken);
        var deleted = await _roomRepository.DeleteAsync(id, cancellationToken);
        if (deleted && current is not null)
        {
            await _adminAuditLogService.RecordForCurrentUserAsync(
                this,
                "Eliminar habitacion",
                AdminAuditDescriptionBuilder.Deleted(
                    "Habitacion",
                    $"Habitacion {current.RoomNumber} (ID {id}), tipo {current.RoomTypeName}, estado {current.RoomStatusName}"),
                cancellationToken);
        }

        return deleted
            ? NoContent()
            : NotFound(new { message = "Habitación no encontrada." });
    }
}

public sealed record RoomRequest(string RoomNumber, int RoomTypeId, int RoomStatusId);
