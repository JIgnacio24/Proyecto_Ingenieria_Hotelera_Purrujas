using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;

namespace Backend_Ingenieria_Purrujas.Application.Quotes;

public class QuoteService : IQuoteService
{
    private readonly IRoomTypeRepository _roomTypeRepository;
    private readonly ISeasonRepository _seasonRepository;
    private const decimal UsdToCrcRate = 500m;
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

    public async Task<QuoteResponseDto> CalculateAsync(
        QuoteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var roomTypeKey = NormalizeRoomTypeKey(request.RoomTypeKey);
        ValidateStayDates(request.StartDate, request.EndDate);
        var currency = NormalizeCurrency(request.Currency);

        var roomType = await _roomTypeRepository.GetByKeyAsync(roomTypeKey, cancellationToken)
            ?? throw new ArgumentException($"Tipo de habitación '{roomTypeKey}' no encontrado.");

        var seasons = await _seasonRepository.GetActiveAsync(cancellationToken);
        var quoteBreakdown = BuildQuoteBreakdown(request.StartDate, request.EndDate, roomType.BasePrice, seasons);

        var (total, basePerNight) = currency == "CRC"
            ? ConvertToCrc(quoteBreakdown.TotalUsd, roomType.BasePrice)
            : (quoteBreakdown.TotalUsd, roomType.BasePrice);

        return new QuoteResponseDto(
            RoomTypeKey: roomTypeKey,
            NightsTotal: quoteBreakdown.TotalNights,
            NightsHigh: quoteBreakdown.HighSeasonNights,
            NightsLow: quoteBreakdown.LowSeasonNights,
            BasePricePerNight: decimal.Round(basePerNight, 2),
            HighSeasonMultiplier: quoteBreakdown.HighestMultiplier,
            Total: decimal.Round(total, 2),
            Currency: currency
        );
    }

    private static (decimal total, decimal basePerNight) ConvertToCrc(decimal totalUsd, decimal baseUsd)
    {
        return (totalUsd * UsdToCrcRate, baseUsd * UsdToCrcRate);
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
            : throw new ArgumentException("La moneda seleccionada no es válida.");
    }

    private static string NormalizeRoomTypeKey(string roomTypeKey)
    {
        if (string.IsNullOrWhiteSpace(roomTypeKey))
        {
            throw new ArgumentException("El tipo de habitación es obligatorio.");
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

            var multiplier = ResolveMultiplier(cursor, seasons);
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

    private static decimal ResolveMultiplier(DateOnly date, IReadOnlyCollection<Season> seasons)
    {
        var matchedSeason = seasons
            .Where(season => date >= season.StartDate && date <= season.EndDate)
            .OrderByDescending(season => season.PercentageChange)
            .FirstOrDefault();

        return matchedSeason is null
            ? 1m
            : 1m + (matchedSeason.PercentageChange / 100m);
    }

    private sealed record QuoteBreakdown(
        int TotalNights,
        int HighSeasonNights,
        int LowSeasonNights,
        decimal TotalUsd,
        decimal HighestMultiplier
    );
}
