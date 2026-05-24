using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IRoomTypeRepository
{
    Task<IReadOnlyList<RoomType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoomType?> GetByIdAsync(int roomTypeId, CancellationToken cancellationToken = default);
    Task<RoomType?> GetByKeyAsync(string roomKey, CancellationToken cancellationToken = default);
    Task<RoomType> CreateAsync(RoomType roomType, CancellationToken cancellationToken = default);
    Task<RoomType?> UpdateAsync(RoomType roomType, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int roomTypeId, CancellationToken cancellationToken = default);
}
