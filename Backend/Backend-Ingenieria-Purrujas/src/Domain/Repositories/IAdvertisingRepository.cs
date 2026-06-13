using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IAdvertisingRepository
{
    Task<IReadOnlyList<Advertising>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Advertising>> GetAllAdminAsync(CancellationToken cancellationToken = default);
    Task<Advertising?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Advertising> CreateAsync(Advertising advertising, CancellationToken cancellationToken = default);
    Task<Advertising?> UpdateAsync(Advertising advertising, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
