namespace Backend_Ingenieria_Purrujas.Application.Quotes;

public record QuoteRequestDto(
    string RoomTypeKey,
    DateOnly StartDate,
    DateOnly EndDate
);
