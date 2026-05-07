using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/about-us-content")]
public class AboutUsContentController : ControllerBase
{
    private readonly IAboutUsPageContentRepository _aboutUsPageContentRepository;

    public AboutUsContentController(IAboutUsPageContentRepository aboutUsPageContentRepository)
    {
        _aboutUsPageContentRepository = aboutUsPageContentRepository;
    }

    [HttpGet]
    public async Task<ActionResult<AboutUsPageContent>> Get(CancellationToken cancellationToken)
    {
        // Endpoint publico consumido por el frontend cliente y la vista previa del admin.
        var content = await _aboutUsPageContentRepository.GetAsync(cancellationToken);
        return Ok(content);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut]
    public async Task<ActionResult<AboutUsPageContent>> Update(
        [FromBody] AboutUsPageContent request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Solo administradores pueden publicar cambios en el JSON de About Us.
            var content = await _aboutUsPageContentRepository.UpsertAsync(request, cancellationToken);
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
