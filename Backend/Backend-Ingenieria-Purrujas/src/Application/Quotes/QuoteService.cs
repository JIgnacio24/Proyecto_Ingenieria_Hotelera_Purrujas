using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;

namespace Backend_Ingenieria_Purrujas.Application.Quotes;

public class QuoteService : IQuoteService
{
    private readonly IRoomTypeRepository _roomTypeRepository;
    private readonly ISeasonRepository _seasonRepository;
    private const decimal UsdToCrcRate = 500m;
    private static readonly HashSet<string> SupportedCurrencies = ["USD", "CRC"];

    public QuoteService(IRoomTypeRepository roomTypeRepository, ISeasonRepository seasonRepository)
    {
        _roomTypeRepository = roomTypeRepository;
        _seasonRepository = seasonRepository;
    }

    public async Task<QuoteResponseDto> CalculateAsync(QuoteRequestDto request, CancellationToken cancellationToken = default)
    {
        var roomTypeKey = NormalizeRoomTypeKey(request.RoomTypeKey);
        ValidateStayDates(request.StartDate, request.EndDate);
        var roomType = await _roomTypeRepository.GetByKeyAsync(roomTypeKey, cancellationToken)
            ?? throw new ArgumentException($"Tipo de habitación '{request.RoomTypeKey}' no encontrado.");
        var currency = NormalizeCurrency(request.Currency);
        var seasons = await _seasonRepository.GetActiveAsync(cancellationToken);
        var quoteBreakdown = BuildQuoteBreakdown(request.StartDate, request.EndDate, roomType.BasePrice, seasons);

        var (total, basePerNight, multiplier) = currency == "CRC"
            ? ConvertToCrc(quoteBreakdown.TotalUsd, roomType.BasePrice, quoteBreakdown.HighestMultiplier)
            : (quoteBreakdown.TotalUsd, roomType.BasePrice, quoteBreakdown.HighestMultiplier);

        return new QuoteResponseDto(
            RoomTypeKey: request.RoomTypeKey,
            NightsTotal: quoteBreakdown.TotalNights,
            NightsHigh: quoteBreakdown.HighSeasonNights,
            NightsLow: quoteBreakdown.LowSeasonNights,
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

    private static QuoteBreakdown BuildQuoteBreakdown(
        DateOnly start,
        DateOnly end,
        decimal basePrice,
        IReadOnlyCollection<Season> seasons)
    {
        var totalNights = 0;
        var highSeasonNights = 0;
        var lowSeasonNights = 0;
        var totalUsd = 0m;
        var highestMultiplier = 1m;

        var cursor = start;
        while (cursor < end)
        {
            totalNights++;

            var matchedSeason = seasons
                .Where(season => cursor >= season.StartDate && cursor <= season.EndDate)
                .OrderByDescending(season => season.PercentageChange)
                .FirstOrDefault();

            var multiplier = matchedSeason is null
                ? 1m
                : 1m + (matchedSeason.PercentageChange / 100m);

            totalUsd += basePrice * multiplier;

            if (multiplier > 1m)
            {
                highSeasonNights++;
            }
            else
            {
                lowSeasonNights++;
            }

            if (multiplier > highestMultiplier)
            {
                highestMultiplier = multiplier;
            }

            cursor = cursor.AddDays(1);
        }

        return new QuoteBreakdown(totalNights, highSeasonNights, lowSeasonNights, totalUsd, highestMultiplier);
    }

    private sealed record QuoteBreakdown(
        int TotalNights,
        int HighSeasonNights,
        int LowSeasonNights,
        decimal TotalUsd,
        decimal HighestMultiplier
    );
}
