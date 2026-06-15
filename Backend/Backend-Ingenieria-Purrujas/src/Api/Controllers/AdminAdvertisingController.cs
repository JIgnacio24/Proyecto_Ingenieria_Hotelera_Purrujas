using System.ComponentModel.DataAnnotations;
using Backend_Ingenieria_Purrujas.Api.Extensions;
using Backend_Ingenieria_Purrujas.Api.Services;
using Backend_Ingenieria_Purrujas.Application.AdminAudit;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/advertising")]
public class AdminAdvertisingController : ControllerBase
{
    private readonly IAdvertisingRepository _advertisingRepository;
    private readonly IAdminAuditLogService _adminAuditLogService;

    public AdminAdvertisingController(
        IAdvertisingRepository advertisingRepository,
        IAdminAuditLogService adminAuditLogService)
    {
        _advertisingRepository = advertisingRepository;
        _adminAuditLogService = adminAuditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Advertising>>> GetAll(CancellationToken ct)
    {
        var items = await _advertisingRepository.GetAllAdminAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Advertising>> GetById(int id, CancellationToken ct)
    {
        var item = await _advertisingRepository.GetByIdAsync(id, ct);
        return item is null
            ? NotFound(new { message = "Anuncio no encontrado." })
            : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Advertising>> Create(
        [FromBody] AdvertisingRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var advertising = new Advertising
            {
                Title       = request.Title,
                Description = request.Description,
                ImageUrl    = request.ImageUrl,
                Link        = request.Link
            };

            var created = await _advertisingRepository.CreateAsync(advertising, ct);

            await _adminAuditLogService.RecordForCurrentUserAsync(
                this,
                "Crear anuncio",
                AdminAuditDescriptionBuilder.Created(
                    "Anuncio",
                    $"{created.Title} (ID {created.AdvertisingId})",
                    [
                        AdminAuditDescriptionBuilder.Value("enlace", created.Link),
                        AdminAuditDescriptionBuilder.Value("imagen", created.ImageUrl ?? "sin imagen")
                    ]),
                ct);

            return CreatedAtAction(nameof(GetById), new { id = created.AdvertisingId }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Advertising>> Update(
        int id, [FromBody] AdvertisingRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var current = await _advertisingRepository.GetByIdAsync(id, ct);
            if (current is null)
                return NotFound(new { message = "Anuncio no encontrado." });

            var advertising = new Advertising
            {
                AdvertisingId = id,
                Title         = request.Title,
                Description   = request.Description,
                ImageUrl      = request.ImageUrl,
                Link          = request.Link,
                IsActive      = request.IsActive
            };

            var updated = await _advertisingRepository.UpdateAsync(advertising, ct);

            if (updated is not null)
            {
                await _adminAuditLogService.RecordForCurrentUserAsync(
                    this,
                    "Actualizar anuncio",
                    AdminAuditDescriptionBuilder.Updated(
                        "Anuncio",
                        $"{current.Title} (ID {id})",
                        current,
                        updated),
                    ct);
            }

            return updated is null
                ? NotFound(new { message = "Anuncio no encontrado." })
                : Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var current = await _advertisingRepository.GetByIdAsync(id, ct);
            await _advertisingRepository.DeleteAsync(id, ct);

            if (current is not null)
            {
                await _adminAuditLogService.RecordForCurrentUserAsync(
                    this,
                    "Eliminar anuncio",
                    AdminAuditDescriptionBuilder.Deleted(
                        "Anuncio",
                        $"{current.Title} (ID {id}), enlace: {current.Link}"),
                    ct);
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed record AdvertisingRequest(
    [Required]
    [StringLength(255, MinimumLength = 1)]
    string Title,

    [StringLength(1000)]
    string? Description,

    [StringLength(500)]
    [Url]
    string? ImageUrl,

    [Required]
    [StringLength(500, MinimumLength = 1)]
    [Url]
    string Link,

    bool IsActive = true);
