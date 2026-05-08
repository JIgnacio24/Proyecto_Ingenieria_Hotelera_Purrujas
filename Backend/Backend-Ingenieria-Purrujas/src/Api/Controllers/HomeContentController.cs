using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/home-content")]
public class HomeContentController : ControllerBase
{
    private readonly IHomePageContentRepository _homePageContentRepository;

    public HomeContentController(IHomePageContentRepository homePageContentRepository)
    {
        _homePageContentRepository = homePageContentRepository;
    }

    [HttpGet]
    public async Task<ActionResult<HomePageContent>> Get(CancellationToken cancellationToken)
    {
        // Endpoint publico usado por el home del cliente y la vista previa del editor.
        var content = await _homePageContentRepository.GetAsync(cancellationToken);
        return Ok(content);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut]
    public async Task<ActionResult<HomePageContent>> Update(
        [FromBody] HomePageContent request,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await _homePageContentRepository.UpsertAsync(request, cancellationToken);
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
