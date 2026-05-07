using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/getting-there-content")]
public class GettingThereContentController : ControllerBase
{
    private readonly IGettingTherePageContentRepository _gettingTherePageContentRepository;

    public GettingThereContentController(IGettingTherePageContentRepository gettingTherePageContentRepository)
    {
        _gettingTherePageContentRepository = gettingTherePageContentRepository;
    }

    [HttpGet]
    public async Task<ActionResult<GettingTherePageContent>> Get(CancellationToken cancellationToken)
    {
        var content = await _gettingTherePageContentRepository.GetAsync(cancellationToken);
        return Ok(content);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut]
    public async Task<ActionResult<GettingTherePageContent>> Update(
        [FromBody] GettingTherePageContent request,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await _gettingTherePageContentRepository.UpsertAsync(request, cancellationToken);
            return Ok(content);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
