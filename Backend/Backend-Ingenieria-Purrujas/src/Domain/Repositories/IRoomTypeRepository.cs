using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IRoomTypeRepository
{
    Task<RoomType?> GetByKeyAsync(string roomKey, CancellationToken cancellationToken = default);
}
