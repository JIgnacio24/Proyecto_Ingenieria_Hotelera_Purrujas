namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public sealed class HomePageContent
{
    // Contrato editable del hero principal de la pagina de inicio.
    public string HeroEyebrow { get; set; } = string.Empty;
    public string HeroTitle { get; set; } = string.Empty;
    public string HeroSubtitle { get; set; } = string.Empty;
}
