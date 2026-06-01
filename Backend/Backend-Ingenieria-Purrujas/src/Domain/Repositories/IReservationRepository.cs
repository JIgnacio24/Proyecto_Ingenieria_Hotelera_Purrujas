using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IReservationRepository
{
    Task<int> CreateAsync(Reservation reservation, Bill bill, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ReservationDetail>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ReservationDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int id, int statusId, CancellationToken cancellationToken = default);
    Task<int?> GetStatusIdByNameAsync(string statusName, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ReservationDetail>> GetDeletedAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, DateTime reservationDate, DateOnly startDate, DateOnly endDate, int roomId, int customerId, int statusId, CancellationToken cancellationToken = default);
    Task UpdateBillAsync(int reservationId, decimal basePrice, decimal seasonAmount, CancellationToken cancellationToken = default);
}
