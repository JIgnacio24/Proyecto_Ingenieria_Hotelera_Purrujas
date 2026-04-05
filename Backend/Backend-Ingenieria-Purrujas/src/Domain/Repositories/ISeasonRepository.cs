using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface ISeasonRepository
{
    Task<IReadOnlyCollection<Season>> GetActiveAsync(CancellationToken cancellationToken = default);
}
