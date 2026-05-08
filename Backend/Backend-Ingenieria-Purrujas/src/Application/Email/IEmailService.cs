using Backend_Ingenieria_Purrujas.Application.Reservations.Dtos;

namespace Backend_Ingenieria_Purrujas.Application.Email;

public interface IEmailService
{
    Task SendReservationConfirmationAsync(ReservationResponseDto reservation, CancellationToken cancellationToken = default);
    Task SendReservationStatusUpdateAsync(ReservationResponseDto reservation, CancellationToken cancellationToken = default);
}
