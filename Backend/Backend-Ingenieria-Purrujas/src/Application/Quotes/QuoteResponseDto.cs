namespace Backend_Ingenieria_Purrujas.Application.Quotes;

public record QuoteResponseDto(
    string RoomTypeKey,
    int NightsTotal,
    int NightsHigh,
    int NightsLow,
    decimal BasePricePerNight,
    decimal HighSeasonMultiplier,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal Total,
    string Currency,
    int PromotionDiscount,
    string? PromotionName
);
