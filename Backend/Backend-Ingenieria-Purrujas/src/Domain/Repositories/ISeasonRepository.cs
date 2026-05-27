using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface ISeasonRepository
{
    /// <summary>Solo temporadas activas — usado por el motor de cotización.</summary>
    Task<IReadOnlyCollection<Season>> GetActiveAsync(CancellationToken cancellationToken = default);

    // ── Admin CRUD ───────────────────────────────────────────────────────────

    Task<IReadOnlyList<Season>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Season?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Season> CreateAsync(Season season, CancellationToken cancellationToken = default);
    Task<Season?> UpdateAsync(Season season, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
