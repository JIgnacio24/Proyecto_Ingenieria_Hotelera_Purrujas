using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/facilities-content")]
public class FacilitiesContentController : ControllerBase
{
    private readonly IFacilitiesPageContentRepository _facilitiesPageContentRepository;

    public FacilitiesContentController(IFacilitiesPageContentRepository facilitiesPageContentRepository)
    {
        _facilitiesPageContentRepository = facilitiesPageContentRepository;
    }

    [HttpGet]
    public async Task<ActionResult<FacilitiesPageContent>> Get(CancellationToken cancellationToken)
    {
        var content = await _facilitiesPageContentRepository.GetAsync(cancellationToken);
        return Ok(content);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut]
    public async Task<ActionResult<FacilitiesPageContent>> Update(
        [FromBody] FacilitiesPageContent request,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await _facilitiesPageContentRepository.UpsertAsync(request, cancellationToken);
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
