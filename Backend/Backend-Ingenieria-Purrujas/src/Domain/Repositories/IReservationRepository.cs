using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IReservationRepository
{
    Task<int> CreateAsync(Reservation reservation, Bill bill, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ReservationDetail>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ReservationDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int id, int statusId, CancellationToken cancellationToken = default);
    Task<int?> GetStatusIdByNameAsync(string statusName, CancellationToken cancellationToken = default);
}
