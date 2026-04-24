using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IFacilitiesPageContentRepository
{
    Task<FacilitiesPageContent> GetAsync(CancellationToken cancellationToken = default);
    Task<FacilitiesPageContent> UpsertAsync(FacilitiesPageContent content, CancellationToken cancellationToken = default);
}
