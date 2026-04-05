using Backend_Ingenieria_Purrujas.Application.Quotes;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuoteController : ControllerBase
{
    private readonly IQuoteService _quoteService;

    public QuoteController(IQuoteService quoteService)
    {
        _quoteService = quoteService;
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<QuoteResponseDto>> Calculate([FromBody] QuoteRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _quoteService.CalculateAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
