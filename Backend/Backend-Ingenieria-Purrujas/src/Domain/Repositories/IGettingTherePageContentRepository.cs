using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IGettingTherePageContentRepository
{
    Task<GettingTherePageContent> GetAsync(CancellationToken cancellationToken = default);
    Task<GettingTherePageContent> UpsertAsync(GettingTherePageContent content, CancellationToken cancellationToken = default);
}
