using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IAboutUsPageContentRepository
{
    // Lee el contenido vigente con fallback para instalaciones sin registro inicial.
    Task<AboutUsPageContent> GetAsync(CancellationToken cancellationToken = default);
    // Publica la version completa editada desde el panel administrativo.
    Task<AboutUsPageContent> UpsertAsync(AboutUsPageContent content, CancellationToken cancellationToken = default);
}
