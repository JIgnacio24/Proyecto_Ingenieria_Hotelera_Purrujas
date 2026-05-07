namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public sealed class AboutUsPageContent
{
    // Contrato editable de la pagina "Sobre Nosotros".
    // Se serializa completo como JSON para mantener el contenido agrupado en una unica fila.
    public string HistoryTag { get; set; } = string.Empty;
    public string HistoryTitle { get; set; } = string.Empty;
    public string HistoryDescription { get; set; } = string.Empty;
    public string HistoryTimelineStartYear { get; set; } = string.Empty;
    public List<string> HistoryMilestones { get; set; } = [];
    public string HistoryTimelineEndLabel { get; set; } = string.Empty;

    public string TeamTag { get; set; } = string.Empty;
    public string TeamTitle { get; set; } = string.Empty;
    public int CollaboratorsCount { get; set; }
    public int LocalTalentPercentage { get; set; }
    public int ExperienceYears { get; set; }

    public string DirectorName { get; set; } = string.Empty;
    public string DirectorTitle { get; set; } = string.Empty;
    public string DirectorBiography { get; set; } = string.Empty;

    public string PhilosophyTitle { get; set; } = string.Empty;
    public string PhilosophyDescription { get; set; } = string.Empty;
    public string PhilosophyQuote { get; set; } = string.Empty;

    public string MvvTag { get; set; } = string.Empty;
    public string MvvTitle { get; set; } = string.Empty;
    // Titulos independientes de las tarjetas para permitir cambios desde el panel admin.
    public string MissionTitle { get; set; } = "Mision";
    public string Mission { get; set; } = string.Empty;
    public string VisionTitle { get; set; } = "Vision";
    public string Vision { get; set; } = string.Empty;
    public string ValuesTitle { get; set; } = "Valores";
    public List<string> Values { get; set; } = [];

    public string GalleryTag { get; set; } = string.Empty;
    public string GalleryTitle { get; set; } = string.Empty;
    public string GallerySubtext { get; set; } = string.Empty;
}
