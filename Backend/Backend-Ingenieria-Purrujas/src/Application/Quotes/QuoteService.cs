using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;

namespace Backend_Ingenieria_Purrujas.Application.Quotes;

public class QuoteService : IQuoteService
{
    private readonly IRoomTypeRepository _roomTypeRepository;
    private readonly ISeasonRepository _seasonRepository;
    private const decimal DefaultHighSeasonMultiplier = 1.25m;
    private const decimal UsdToCrcRate = 500m;
    private static readonly int[] HighSeasonMonths = { 0, 6, 7, 11 }; // Jan, Jul, Aug, Dec
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD",
        "CRC"
    };

    public QuoteService(IRoomTypeRepository roomTypeRepository, ISeasonRepository seasonRepository)
    {
        _roomTypeRepository = roomTypeRepository;
        _seasonRepository = seasonRepository;
    }

    public async Task<QuoteResponseDto> CalculateAsync(QuoteRequestDto request, CancellationToken cancellationToken = default)
    {
        var roomTypeKey = NormalizeRoomTypeKey(request.RoomTypeKey);
        ValidateStayDates(request.StartDate, request.EndDate);
        var currency = NormalizeCurrency(request.Currency);

        var roomType = await _roomTypeRepository.GetByKeyAsync(roomTypeKey, cancellationToken)
            ?? throw new ArgumentException($"Tipo de habitacion '{roomTypeKey}' no encontrado.");

        var highMultiplier = await ResolveHighMultiplier(cancellationToken);
        var (totalNights, highNights, lowNights) = CountNights(request.StartDate, request.EndDate);

        var priceHigh = roomType.BasePrice * highMultiplier;
        var totalUsd = (lowNights * roomType.BasePrice) + (highNights * priceHigh);

        var (total, basePerNight, multiplier) = currency == "CRC"
            ? ConvertToCrc(totalUsd, roomType.BasePrice, highMultiplier)
            : (totalUsd, roomType.BasePrice, highMultiplier);

        return new QuoteResponseDto(
            RoomTypeKey: roomTypeKey,
            NightsTotal: totalNights,
            NightsHigh: highNights,
            NightsLow: lowNights,
            BasePricePerNight: basePerNight,
            HighSeasonMultiplier: multiplier,
            Total: decimal.Round(total, 2),
            Currency: currency
        );
    }

    private static (decimal total, decimal basePerNight, decimal multiplier) ConvertToCrc(
        decimal totalUsd,
        decimal baseUsd,
        decimal multiplier)
    {
        return (totalUsd * UsdToCrcRate, baseUsd * UsdToCrcRate, multiplier);
    }

    private static string NormalizeCurrency(string requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            throw new ArgumentException("La moneda es obligatoria.");
        }

        var upper = requested.Trim().ToUpperInvariant();
        return SupportedCurrencies.Contains(upper)
            ? upper
            : throw new ArgumentException("La moneda seleccionada no es valida.");
    }

    private static string NormalizeRoomTypeKey(string roomTypeKey)
    {
        if (string.IsNullOrWhiteSpace(roomTypeKey))
        {
            throw new ArgumentException("El tipo de habitacion es obligatorio.");
        }

        return roomTypeKey.Trim().ToLowerInvariant();
    }

    private static void ValidateStayDates(DateOnly startDate, DateOnly endDate)
    {
        if (startDate == default || endDate == default)
        {
            throw new ArgumentException("Debe seleccionar una fecha de entrada y una fecha de salida.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (startDate < today)
        {
            throw new ArgumentException("La fecha de entrada no puede estar en el pasado.");
        }

        if (endDate <= startDate)
        {
            throw new ArgumentException("La fecha de salida debe ser posterior a la de entrada.");
        }
    }

    private async Task<decimal> ResolveHighMultiplier(CancellationToken cancellationToken)
    {
        var seasons = await _seasonRepository.GetActiveAsync(cancellationToken);
        var percentage = seasons.FirstOrDefault()?.PercentageChange;
        return percentage.HasValue
            ? 1 + (percentage.Value / 100m)
            : DefaultHighSeasonMultiplier;
    }

    private static (int total, int high, int low) CountNights(DateOnly start, DateOnly end)
    {
        var total = 0;
        var high = 0;
        var low = 0;

        var cursor = start;
        while (cursor < end)
        {
            total++;
            if (IsHighSeason(cursor))
            {
                high++;
            }
            else
            {
                low++;
            }

            cursor = cursor.AddDays(1);
        }

        return (total, high, low);
    }

    private static bool IsHighSeason(DateOnly date)
    {
        var month = date.Month - 1;
        var day = date.Day;

        if (HighSeasonMonths.Contains(month))
        {
            return true;
        }

        return (month == 2 && day >= 29) || (month == 3 && day <= 5);
    }
}
