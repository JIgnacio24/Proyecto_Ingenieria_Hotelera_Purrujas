using Backend_Ingenieria_Purrujas.Application.Reservations.Dtos;

namespace Backend_Ingenieria_Purrujas.Application.Reservations;

public interface IReservationService
{
    Task<AvailabilityResponseDto> CheckAvailabilityAsync(string roomTypeKey, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<ReservationResponseDto> CreateAsync(CreateReservationRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ReservationResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ReservationResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ReservationResponseDto>> GetDeletedAsync(CancellationToken cancellationToken = default);
    Task<ReservationResponseDto> UpdateAsync(int id, UpdateReservationRequestDto request, CancellationToken cancellationToken = default);
}
