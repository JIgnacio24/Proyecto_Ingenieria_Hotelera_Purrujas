using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IPromotionRepository
{
    Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Promotion> CreateAsync(Promotion promotion, CancellationToken cancellationToken = default);
    Task<Promotion?> UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
