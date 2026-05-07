using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IRoomAvailabilityRepository
{
    Task<RoomAvailabilitySummary> GetTodayAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<RoomAvailabilitySearchResult> SearchAsync(
        DateOnly startDate,
        DateOnly endDate,
        int? roomTypeId,
        CancellationToken cancellationToken = default);
}
