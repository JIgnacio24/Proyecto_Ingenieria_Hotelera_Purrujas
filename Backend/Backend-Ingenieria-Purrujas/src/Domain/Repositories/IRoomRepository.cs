using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IRoomRepository
{
    Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Room?> GetByIdAsync(int roomId, CancellationToken cancellationToken = default);
    Task<Room> CreateAsync(Room room, CancellationToken cancellationToken = default);
    Task<Room?> UpdateAsync(Room room, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomStatusOption>> GetRoomStatusesAsync(CancellationToken cancellationToken = default);
    Task<Room?> GetFirstAvailableAsync(string roomTypeKey, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<int> CountAvailableAsync(string roomTypeKey, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<string> GetRoomTypeNameAsync(string roomTypeKey, CancellationToken cancellationToken = default);
    Task<string?> GetRoomTypeKeyByRoomIdAsync(int roomId, CancellationToken cancellationToken = default);
}
