using Backend_Ingenieria_Purrujas.Api.Extensions;
using Backend_Ingenieria_Purrujas.Api.Services;
using Backend_Ingenieria_Purrujas.Application.AdminAudit;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/home-content")]
public class HomeContentController : ControllerBase
{
    private readonly IAdminAuditLogService _adminAuditLogService;
    private readonly IHomePageContentRepository _homePageContentRepository;

    public HomeContentController(
        IHomePageContentRepository homePageContentRepository,
        IAdminAuditLogService adminAuditLogService)
    {
        _homePageContentRepository = homePageContentRepository;
        _adminAuditLogService = adminAuditLogService;
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
            var previousContent = await _homePageContentRepository.GetAsync(cancellationToken);
            var content = await _homePageContentRepository.UpsertAsync(request, cancellationToken);
            await _adminAuditLogService.RecordForCurrentUserAsync(
                this,
                "Actualizar inicio",
                AdminAuditDescriptionBuilder.ContentUpdated("inicio", previousContent, content),
                cancellationToken);

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
