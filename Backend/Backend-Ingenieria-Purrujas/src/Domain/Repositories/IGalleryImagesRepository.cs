using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IGalleryImagesRepository
{
    Task<IReadOnlyList<GalleryImage>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GalleryImage?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GalleryImage> UpdateAsync(GalleryImage image, CancellationToken cancellationToken = default);
}
