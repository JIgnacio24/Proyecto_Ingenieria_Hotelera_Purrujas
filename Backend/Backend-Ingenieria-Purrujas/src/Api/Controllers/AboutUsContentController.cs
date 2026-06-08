using Backend_Ingenieria_Purrujas.Api.Extensions;
using Backend_Ingenieria_Purrujas.Api.Services;
using Backend_Ingenieria_Purrujas.Application.AdminAudit;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/about-us-content")]
public class AboutUsContentController : ControllerBase
{
    private readonly IAdminAuditLogService _adminAuditLogService;
    private readonly IAboutUsPageContentRepository _aboutUsPageContentRepository;

    public AboutUsContentController(
        IAboutUsPageContentRepository aboutUsPageContentRepository,
        IAdminAuditLogService adminAuditLogService)
    {
        _aboutUsPageContentRepository = aboutUsPageContentRepository;
        _adminAuditLogService = adminAuditLogService;
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
            var previousContent = await _aboutUsPageContentRepository.GetAsync(cancellationToken);
            var content = await _aboutUsPageContentRepository.UpsertAsync(request, cancellationToken);
            await _adminAuditLogService.RecordForCurrentUserAsync(
                this,
                "Actualizar Sobre nosotros",
                AdminAuditDescriptionBuilder.ContentUpdated("Sobre nosotros", previousContent, content),
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
