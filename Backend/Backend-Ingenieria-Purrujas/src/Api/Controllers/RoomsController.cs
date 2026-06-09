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
    private readonly IRoomRepository _roomRepository;

    public RoomsController(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
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
            var updated = await _roomRepository.UpdateAsync(
                new Room
                {
                    RoomId = id,
                    RoomNumber = request.RoomNumber,
                    RoomTypeId = request.RoomTypeId,
                    RoomStatusId = request.RoomStatusId
                },
                cancellationToken);

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
        var deleted = await _roomRepository.DeleteAsync(id, cancellationToken);
        return deleted
            ? NoContent()
            : NotFound(new { message = "Habitación no encontrada." });
    }
}

public sealed record RoomRequest(string RoomNumber, int RoomTypeId, int RoomStatusId);
