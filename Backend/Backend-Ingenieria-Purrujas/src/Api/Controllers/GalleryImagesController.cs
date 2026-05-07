using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/gallery-images")]
public class GalleryImagesController : ControllerBase
{
    private readonly IGalleryImagesRepository _galleryImagesRepository;
    private readonly IWebHostEnvironment _environment;

    public GalleryImagesController(
        IGalleryImagesRepository galleryImagesRepository,
        IWebHostEnvironment environment)
    {
        _galleryImagesRepository = galleryImagesRepository;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GalleryImage>>> Get(
        CancellationToken cancellationToken)
    {
        var images = await _galleryImagesRepository.GetAllAsync(cancellationToken);
        return Ok(images);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    [RequestSizeLimit(12_000_000)]
    public async Task<ActionResult<GalleryImage>> Update(
        int id,
        [FromForm] string name,
        [FromForm] string alt,
        [FromForm] string caption,
        [FromForm] string category,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentImage = await _galleryImagesRepository.GetByIdAsync(id, cancellationToken);
            if (currentImage is null)
            {
                return NotFound(new { message = "La imagen de galeria no existe." });
            }

            var normalizedName = RequireValue(name, "El nombre de la imagen es requerido.");
            var normalizedAlt = RequireValue(alt, "El texto alternativo es requerido.");
            var normalizedCaption = RequireValue(caption, "La leyenda de la imagen es requerida.");
            var normalizedCategory = NormalizeCategory(category);
            var src = currentImage.Src;

            if (file is not null && file.Length > 0)
            {
                src = await SaveGalleryFileAsync(file, cancellationToken);
            }

            var updatedImage = await _galleryImagesRepository.UpdateAsync(
                new GalleryImage
                {
                    Id = id,
                    Name = normalizedName,
                    Src = src,
                    Alt = normalizedAlt,
                    Caption = normalizedCaption,
                    Category = normalizedCategory,
                    IsActive = currentImage.IsActive
                },
                cancellationToken);

            return Ok(updatedImage);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<string> SaveGalleryFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("El archivo debe ser una imagen.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Solo se permiten imagenes JPG, PNG o WEBP.");
        }

        var webRootPath = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadsDirectory = Path.Combine(webRootPath, "uploads", "gallery");
        Directory.CreateDirectory(uploadsDirectory);

        var baseFileName = Path.GetFileNameWithoutExtension(file.FileName);
        var safeFileName = string.Concat(
            baseFileName
                .Normalize()
                .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_'));

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "gallery";
        }

        var fileName = $"{safeFileName}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}{extension}";
        var filePath = Path.Combine(uploadsDirectory, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/gallery/{fileName}";
    }

    private static string RequireValue(string? value, string errorMessage)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException(errorMessage);
        }

        return normalized;
    }

    private static string NormalizeCategory(string? category)
    {
        var normalized = category?.Trim().ToLowerInvariant();
        return normalized is "hotel" or "lugares" ? normalized : "hotel";
    }
}
