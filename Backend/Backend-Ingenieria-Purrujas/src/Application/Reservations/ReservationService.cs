using Backend_Ingenieria_Purrujas.Application.Email;
using Backend_Ingenieria_Purrujas.Application.Quotes;
using Backend_Ingenieria_Purrujas.Application.Reservations.Dtos;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Backend_Ingenieria_Purrujas.Application.Reservations;

public class ReservationService(
    IReservationRepository reservationRepository,
    IRoomRepository roomRepository,
    ICustomerRepository customerRepository,
    IQuoteService quoteService,
    IEmailService emailService,
    ILogger<ReservationService> logger
) : IReservationService
{
    private const decimal UsdToCrcRate = 500m;

    public async Task<AvailabilityResponseDto> CheckAvailabilityAsync(
        string roomTypeKey, DateOnly startDate, DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var count = await roomRepository.CountAvailableAsync(roomTypeKey, startDate, endDate, cancellationToken);
        var typeName = await roomRepository.GetRoomTypeNameAsync(roomTypeKey, cancellationToken);

        return new AvailabilityResponseDto
        {
            IsAvailable    = count > 0,
            AvailableRooms = count,
            RoomTypeName   = typeName,
            StartDate      = startDate,
            EndDate        = endDate
        };
    }

    public async Task<ReservationResponseDto> CreateAsync(
        CreateReservationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (request.StartDate < today)
            throw new ArgumentException("La fecha de entrada no puede ser anterior a hoy.");
        if (request.EndDate <= request.StartDate)
            throw new ArgumentException("La fecha de salida debe ser posterior a la fecha de entrada.");

        var currency = request.Currency.ToUpperInvariant() is "USD" or "CRC"
            ? request.Currency.ToUpperInvariant()
            : "USD";

        var quote = await quoteService.CalculateAsync(
            new QuoteRequestDto(request.RoomTypeKey, request.StartDate, request.EndDate, "USD"),
            cancellationToken);

        var customer = await customerRepository.GetByEmailAsync(request.Email, cancellationToken)
            ?? await customerRepository.CreateAsync(
                request.Name, request.LastName, request.Email,
                request.Phone, request.CreditCard, cancellationToken);

        var room = await roomRepository.GetFirstAvailableAsync(
            request.RoomTypeKey, request.StartDate, request.EndDate, cancellationToken)
            ?? throw new InvalidOperationException("No hay habitaciones disponibles de ese tipo para las fechas indicadas.");

        var reservation = new Reservation
        {
            ReservationDate     = DateTime.UtcNow,
            StartDate           = request.StartDate,
            EndDate             = request.EndDate,
            CustomerId          = customer.CustomerId,
            RoomId              = room.RoomId,
            ReservationStatusId = 1,
            IsActive            = true
        };

        var seasonAmount = quote.Total - (quote.BasePricePerNight * quote.NightsTotal);
        var bill = new Bill
        {
            BasePrice    = quote.BasePricePerNight * quote.NightsTotal,
            Discount     = 0m,
            SeasonAmount = seasonAmount
        };

        var reservationId = await reservationRepository.CreateAsync(reservation, bill, cancellationToken);

        var totalUsd = quote.Total;
        var totalCrc = totalUsd * UsdToCrcRate;

        var dto = new ReservationResponseDto
        {
            ReservationId    = reservationId,
            RoomId           = room.RoomId,
            RoomNumber       = room.RoomNumber,
            RoomTypeName     = room.RoomTypeName,
            CustomerFullName = $"{customer.Name} {customer.LastName}",
            CustomerEmail    = customer.Email,
            StartDate        = request.StartDate,
            EndDate          = request.EndDate,
            NightsTotal      = quote.NightsTotal,
            NightsHigh       = quote.NightsHigh,
            NightsLow        = quote.NightsLow,
            TotalUsd         = totalUsd,
            TotalCrc         = totalCrc,
            Currency         = currency,
            Status           = "Pendiente",
            CreatedAt        = DateTime.UtcNow
        };

        try
        {
            await emailService.SendReservationConfirmationAsync(dto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al enviar correo de confirmación para reserva #{Id}.", reservationId);
        }

        return dto;
    }

    public async Task<IReadOnlyCollection<ReservationResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var details = await reservationRepository.GetAllAsync(cancellationToken);
        return details.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<ReservationResponseDto?> GetByIdAsync(int id,
        CancellationToken cancellationToken = default)
    {
        var detail = await reservationRepository.GetByIdAsync(id, cancellationToken);
        return detail is null ? null : MapToDto(detail);
    }

    public async Task UpdateStatusAsync(int id, string status,
        CancellationToken cancellationToken = default)
    {
        var allowedStatuses = new[] { "Pendiente", "Confirmada", "Finalizada", "Cancelada" };
        if (!allowedStatuses.Contains(status))
            throw new ArgumentException($"Estado no válido: {status}. Valores permitidos: {string.Join(", ", allowedStatuses)}");

        var statusId = await reservationRepository.GetStatusIdByNameAsync(status, cancellationToken)
            ?? throw new KeyNotFoundException($"Estado '{status}' no encontrado en la base de datos.");

        var needsEmail = status is "Confirmada" or "Cancelada";
        var detail = needsEmail
            ? await reservationRepository.GetByIdAsync(id, cancellationToken)
            : null;

        await reservationRepository.UpdateStatusAsync(id, statusId, cancellationToken);

        if (detail is not null)
        {
            var dto = MapToDto(detail) with { Status = status };
            try
            {
                await emailService.SendReservationStatusUpdateAsync(dto, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al enviar correo de estado para reserva #{Id}.", id);
            }
        }
    }

    public async Task<ReservationResponseDto> UpdateAsync(int id, UpdateReservationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.EndDate <= request.StartDate)
            throw new ArgumentException("La fecha de salida debe ser posterior a la fecha de entrada.");

        var current = await reservationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Reserva #{id} no encontrada.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (current.EndDate < today)
            throw new InvalidOperationException("No se pueden modificar reservaciones cuya fecha de salida ya pasó.");

        var roomTypeKey = await roomRepository.GetRoomTypeKeyByRoomIdAsync(request.RoomId, cancellationToken)
            ?? throw new ArgumentException("La habitación seleccionada no fue encontrada.");

        var quote = await quoteService.CalculateAsync(
            new QuoteRequestDto(roomTypeKey, request.StartDate, request.EndDate, "USD"),
            cancellationToken,
            allowPastDates: true);

        await reservationRepository.UpdateAsync(
            id,
            current.ReservationDate,
            request.StartDate,
            request.EndDate,
            request.RoomId,
            current.CustomerId,
            current.ReservationStatusId,
            cancellationToken);

        var basePrice = quote.BasePricePerNight * quote.NightsTotal;
        var seasonAmount = quote.Total - basePrice;
        await reservationRepository.UpdateBillAsync(id, basePrice, seasonAmount, cancellationToken);

        var updated = await reservationRepository.GetByIdAsync(id, cancellationToken);
        return MapToDto(updated!);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var detail = await reservationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Reserva #{id} no encontrada o ya fue eliminada.");

        if (detail.ReservationStatusName != "Cancelada")
            throw new InvalidOperationException("Solo se pueden eliminar reservaciones con estado Cancelada.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (detail.EndDate < today)
            throw new InvalidOperationException("No se pueden eliminar reservaciones cuya fecha de salida ya pasó.");

        await reservationRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReservationResponseDto>> GetDeletedAsync(CancellationToken cancellationToken = default)
    {
        var details = await reservationRepository.GetDeletedAsync(cancellationToken);
        return details.Select(MapToDto).ToList().AsReadOnly();
    }

    private static ReservationResponseDto MapToDto(ReservationDetail d)
    {
        var nights = d.EndDate.DayNumber - d.StartDate.DayNumber;
        var totalUsd = d.BasePrice + d.SeasonAmount - d.Discount;
        return new ReservationResponseDto
        {
            ReservationId    = d.ReservationId,
            RoomId           = d.RoomId,
            RoomNumber       = d.RoomNumber,
            RoomTypeName     = d.RoomTypeName,
            CustomerFullName = $"{d.CustomerName} {d.CustomerLastName}",
            CustomerEmail    = d.CustomerEmail,
            StartDate        = d.StartDate,
            EndDate          = d.EndDate,
            NightsTotal      = nights,
            NightsHigh       = 0,
            NightsLow        = nights,
            TotalUsd         = totalUsd,
            TotalCrc         = totalUsd * 500m,
            Currency         = "USD",
            Status           = d.ReservationStatusName,
            CreatedAt        = d.ReservationDate
        };
    }
}
