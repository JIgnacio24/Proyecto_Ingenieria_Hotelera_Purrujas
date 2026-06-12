using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/room-types")]
public class PublicRoomTypesController : ControllerBase
{
    private readonly IRoomTypeRepository _roomTypeRepository;

    public PublicRoomTypesController(IRoomTypeRepository roomTypeRepository)
    {
        _roomTypeRepository = roomTypeRepository;
    }

    // GET api/room-types
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomType>>> GetAll(CancellationToken cancellationToken)
    {
        var roomTypes = await _roomTypeRepository.GetAllAsync(cancellationToken);
        return Ok(roomTypes);
    }
}
