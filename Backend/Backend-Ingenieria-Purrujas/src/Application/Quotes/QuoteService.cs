using System.Diagnostics;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Backend_Ingenieria_Purrujas.Application.Quotes;

public class QuoteService : IQuoteService
{
    private readonly IRoomTypeRepository _roomTypeRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<QuoteService> _logger;
    private const decimal UsdToCrcRate = 500m;

    // Los datos de referencia (temporadas, promociones, tipos de habitación) cambian
    // con muy poca frecuencia desde el panel de administración. Cachearlos unos segundos
    // elimina las consultas repetidas cuando el usuario cambia moneda/fechas/habitación.
    private static readonly TimeSpan ReferenceDataTtl = TimeSpan.FromSeconds(60);
    private const string SeasonsCacheKey = "quote:seasons:active";
    private const string PromotionsCacheKey = "quote:promotions:all";

    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD",
        "CRC"
    };

    public QuoteService(
        IRoomTypeRepository roomTypeRepository,
        ISeasonRepository seasonRepository,
        IPromotionRepository promotionRepository,
        IMemoryCache cache,
        ILogger<QuoteService> logger)
    {
        _roomTypeRepository = roomTypeRepository;
        _seasonRepository = seasonRepository;
        _promotionRepository = promotionRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<QuoteResponseDto> CalculateAsync(
        QuoteRequestDto request,
        CancellationToken cancellationToken = default,
        bool allowPastDates = false)
    {
        var totalStopwatch = Stopwatch.StartNew();

        var roomTypeKey = NormalizeRoomTypeKey(request.RoomTypeKey);
        ValidateStayDates(request.StartDate, request.EndDate, allowPastDates);
        var currency = NormalizeCurrency(request.Currency);

        // Las tres lecturas son independientes entre sí: se lanzan en paralelo
        // (cada repositorio usa su propia conexión) en lugar de en secuencia.
        var fetchStopwatch = Stopwatch.StartNew();
        var roomTypeTask = GetRoomTypeCachedAsync(roomTypeKey, cancellationToken);
        var seasonsTask = GetActiveSeasonsCachedAsync(cancellationToken);
        var promotionsTask = GetPromotionsCachedAsync(cancellationToken);
        await Task.WhenAll(roomTypeTask, seasonsTask, promotionsTask);
        fetchStopwatch.Stop();

        var roomType = roomTypeTask.Result
            ?? throw new ArgumentException($"Tipo de habitación '{roomTypeKey}' no encontrado.");
        var seasons = seasonsTask.Result;
        var promotions = promotionsTask.Result;

        var computeStopwatch = Stopwatch.StartNew();
        var quoteBreakdown = BuildQuoteBreakdown(request.StartDate, request.EndDate, roomType.BasePrice, seasons);
        var promotion = ResolvePromotion(request.StartDate, request.EndDate, roomType.RoomTypeId, promotions);
        var discountUsd = promotion is null
            ? 0m
            : decimal.Round(quoteBreakdown.TotalUsd * promotion.Discount / 100m, 2);
        var totalUsd = quoteBreakdown.TotalUsd - discountUsd;

        var conversionRate = currency == "CRC" ? UsdToCrcRate : 1m;
        var subtotal = quoteBreakdown.TotalUsd * conversionRate;
        var discountAmount = discountUsd * conversionRate;
        var total = totalUsd * conversionRate;
        var basePerNight = roomType.BasePrice * conversionRate;
        computeStopwatch.Stop();
        totalStopwatch.Stop();

        _logger.LogInformation(
            "Cotización calculada: total={TotalMs}ms (datos={FetchMs}ms, cálculo={ComputeMs}ms) " +
            "[habitación={RoomTypeKey}, noches={Nights}, moneda={Currency}]",
            totalStopwatch.ElapsedMilliseconds,
            fetchStopwatch.ElapsedMilliseconds,
            computeStopwatch.ElapsedMilliseconds,
            roomTypeKey,
            quoteBreakdown.TotalNights,
            currency);

        return new QuoteResponseDto(
            RoomTypeKey: roomTypeKey,
            NightsTotal: quoteBreakdown.TotalNights,
            NightsHigh: quoteBreakdown.HighSeasonNights,
            NightsLow: quoteBreakdown.LowSeasonNights,
            BasePricePerNight: decimal.Round(basePerNight, 2),
            HighSeasonMultiplier: quoteBreakdown.HighestMultiplier,
            Subtotal: decimal.Round(subtotal, 2),
            DiscountAmount: decimal.Round(discountAmount, 2),
            Total: decimal.Round(total, 2),
            Currency: currency,
            PromotionDiscount: promotion?.Discount ?? 0,
            PromotionName: promotion?.Name
        );
    }

    // ── Lecturas cacheadas de datos de referencia ────────────────────────────
    // TTL corto: el resultado es idéntico al de la BD dentro de la ventana de
    // caché; los cambios del administrador se reflejan en a lo sumo 60 s.

    private Task<IReadOnlyCollection<Season>> GetActiveSeasonsCachedAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(SeasonsCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ReferenceDataTtl;
            return _seasonRepository.GetActiveAsync(cancellationToken);
        })!;
    }

    private Task<IReadOnlyList<Promotion>> GetPromotionsCachedAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(PromotionsCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ReferenceDataTtl;
            return _promotionRepository.GetAllAsync(cancellationToken);
        })!;
    }

    private Task<RoomType?> GetRoomTypeCachedAsync(string roomTypeKey, CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync($"quote:roomtype:{roomTypeKey}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ReferenceDataTtl;
            return _roomTypeRepository.GetByKeyAsync(roomTypeKey, cancellationToken);
        });
    }

    private static Promotion? ResolvePromotion(
        DateOnly start,
        DateOnly end,
        int roomTypeId,
        IReadOnlyCollection<Promotion> promotions)
    {
        return promotions
            .Where(promotion =>
                promotion.IsActive &&
                promotion.RoomTypeId == roomTypeId &&
                promotion.StartDate < end &&
                promotion.EndDate >= start)
            .OrderByDescending(promotion => promotion.Discount)
            .ThenBy(promotion => promotion.PromotionId)
            .FirstOrDefault();
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

    private static void ValidateStayDates(DateOnly startDate, DateOnly endDate, bool allowPastDates = false)
    {
        if (startDate == default || endDate == default)
        {
            throw new ArgumentException("Debe seleccionar una fecha de entrada y una fecha de salida.");
        }

        if (!allowPastDates)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (startDate < today)
            {
                throw new ArgumentException("La fecha de entrada no puede estar en el pasado.");
            }
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
