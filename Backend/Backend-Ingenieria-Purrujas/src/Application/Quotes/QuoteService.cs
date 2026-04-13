using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using System.Linq;

namespace Backend_Ingenieria_Purrujas.Application.Quotes;

public class QuoteService : IQuoteService
{
    private readonly IRoomTypeRepository _roomTypeRepository;
    private readonly ISeasonRepository _seasonRepository;
    private const decimal DefaultHighSeasonMultiplier = 1.25m;
    private const decimal UsdToCrcRate = 500m;
    private static readonly int[] HighSeasonMonths = { 0, 6, 7, 11 }; // Jan, Jul, Aug, Dec

    public QuoteService(IRoomTypeRepository roomTypeRepository, ISeasonRepository seasonRepository)
    {
        _roomTypeRepository = roomTypeRepository;
        _seasonRepository = seasonRepository;
    }

    public async Task<QuoteResponseDto> CalculateAsync(QuoteRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.EndDate <= request.StartDate)
        {
          throw new ArgumentException("La fecha de salida debe ser posterior a la de entrada.");
        }

        var roomType = await _roomTypeRepository.GetByKeyAsync(request.RoomTypeKey, cancellationToken)
                        ?? throw new ArgumentException($"Tipo de habitación '{request.RoomTypeKey}' no encontrado.");

        var currency = NormalizeCurrency(request.Currency);

        var highMultiplier = await ResolveHighMultiplier(cancellationToken);

        var (totalNights, highNights, lowNights) = CountNights(request.StartDate, request.EndDate);

        var priceHigh = roomType.BasePrice * highMultiplier;
        var totalUsd = (lowNights * roomType.BasePrice) + (highNights * priceHigh);

        var (total, basePerNight, multiplier) = currency == "CRC"
            ? ConvertToCrc(totalUsd, roomType.BasePrice, highMultiplier)
            : (totalUsd, roomType.BasePrice, highMultiplier);

        return new QuoteResponseDto(
            RoomTypeKey: request.RoomTypeKey,
            NightsTotal: totalNights,
            NightsHigh: highNights,
            NightsLow: lowNights,
            BasePricePerNight: basePerNight,
            HighSeasonMultiplier: multiplier,
            Total: decimal.Round(total, 2),
            Currency: currency
        );
    }

    private static (decimal total, decimal basePerNight, decimal multiplier) ConvertToCrc(decimal totalUsd, decimal baseUsd, decimal multiplier)
    {
        return (totalUsd * UsdToCrcRate, baseUsd * UsdToCrcRate, multiplier);
    }

    private static string NormalizeCurrency(string requested)
    {
        if (string.IsNullOrWhiteSpace(requested)) return "USD";
        var upper = requested.Trim().ToUpperInvariant();
        return upper switch
        {
            "USD" => "USD",
            "CRC" => "CRC",
            _ => "USD"
        };
    }

    private async Task<decimal> ResolveHighMultiplier(CancellationToken cancellationToken)
    {
        var seasons = await _seasonRepository.GetActiveAsync(cancellationToken);
        // We pick the first active season percentage, fallback to default 25%
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
            if (IsHighSeason(cursor)) high++; else low++;
            cursor = cursor.AddDays(1);
        }

        return (total, high, low);
    }

    private static bool IsHighSeason(DateOnly date)
    {
        var mes = date.Month - 1; // DateOnly Month is 1-12; align to 0-based months used before
        var dia = date.Day;

        if (HighSeasonMonths.Contains(mes)) return true;

        // Semana Santa aproximada (2026: 29 marzo - 5 abril)
        return (mes == 2 && dia >= 29) || (mes == 3 && dia <= 5);
    }
}
