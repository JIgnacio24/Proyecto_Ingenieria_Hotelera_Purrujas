using Backend_Ingenieria_Purrujas.Api.Extensions;
using Backend_Ingenieria_Purrujas.Api.Services;
using Backend_Ingenieria_Purrujas.Application.AdminAudit;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/getting-there-content")]
public class GettingThereContentController : ControllerBase
{
    private readonly IAdminAuditLogService _adminAuditLogService;
    private readonly IGettingTherePageContentRepository _gettingTherePageContentRepository;

    public GettingThereContentController(
        IGettingTherePageContentRepository gettingTherePageContentRepository,
        IAdminAuditLogService adminAuditLogService)
    {
        _gettingTherePageContentRepository = gettingTherePageContentRepository;
        _adminAuditLogService = adminAuditLogService;
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
            var previousContent = await _gettingTherePageContentRepository.GetAsync(cancellationToken);
            var content = await _gettingTherePageContentRepository.UpsertAsync(request, cancellationToken);
            await _adminAuditLogService.RecordForCurrentUserAsync(
                this,
                "Actualizar como llegar",
                AdminAuditDescriptionBuilder.ContentUpdated("como llegar", previousContent, content),
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
