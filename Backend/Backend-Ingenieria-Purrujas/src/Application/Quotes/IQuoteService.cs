namespace Backend_Ingenieria_Purrujas.Application.Quotes;

public interface IQuoteService
{
    Task<QuoteResponseDto> CalculateAsync(QuoteRequestDto request, CancellationToken cancellationToken = default, bool allowPastDates = false);
}
