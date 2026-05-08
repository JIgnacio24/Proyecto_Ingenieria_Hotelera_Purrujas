using Backend_Ingenieria_Purrujas.Application.Quotes;
using Backend_Ingenieria_Purrujas.Application.Reservations.Dtos;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;

namespace Backend_Ingenieria_Purrujas.Application.Reservations;

public class ReservationService(
    IReservationRepository reservationRepository,
    IRoomRepository roomRepository,
    ICustomerRepository customerRepository,
    IQuoteService quoteService
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

        return new ReservationResponseDto
        {
            ReservationId    = reservationId,
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

        await reservationRepository.UpdateStatusAsync(id, statusId, cancellationToken);
    }

    private static ReservationResponseDto MapToDto(ReservationDetail d)
    {
        var nights = d.EndDate.DayNumber - d.StartDate.DayNumber;
        var totalUsd = d.BasePrice + d.SeasonAmount - d.Discount;
        return new ReservationResponseDto
        {
            ReservationId    = d.ReservationId,
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
