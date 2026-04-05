namespace Backend_Ingenieria_Purrujas.Application.Quotes;

public record QuoteResponseDto(
    string RoomTypeKey,
    int NightsTotal,
    int NightsHigh,
    int NightsLow,
    decimal BasePricePerNight,
    decimal HighSeasonMultiplier,
    decimal Total,
    string Currency
);
