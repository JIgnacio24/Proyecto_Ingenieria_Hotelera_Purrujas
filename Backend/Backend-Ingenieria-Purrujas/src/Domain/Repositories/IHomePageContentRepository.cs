using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IHomePageContentRepository
{
    // Lee el contenido vigente del inicio con fallback para instalaciones sin registro inicial.
    Task<HomePageContent> GetAsync(CancellationToken cancellationToken = default);
    // Publica la version completa editada desde el panel administrativo.
    Task<HomePageContent> UpsertAsync(HomePageContent content, CancellationToken cancellationToken = default);
}
