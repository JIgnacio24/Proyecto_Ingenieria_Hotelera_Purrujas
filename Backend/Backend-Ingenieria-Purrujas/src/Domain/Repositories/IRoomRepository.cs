using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IRoomRepository
{
    Task<Room?> GetFirstAvailableAsync(string roomTypeKey, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<int> CountAvailableAsync(string roomTypeKey, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<string> GetRoomTypeNameAsync(string roomTypeKey, CancellationToken cancellationToken = default);
    Task<string?> GetRoomTypeKeyByRoomIdAsync(int roomId, CancellationToken cancellationToken = default);
}
