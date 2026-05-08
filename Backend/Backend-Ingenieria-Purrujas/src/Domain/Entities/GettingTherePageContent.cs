namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public sealed class GettingTherePageContent
{
    public string SectionTag { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public string SectionSubtext { get; set; } = string.Empty;
    public string CoordinatesTitle { get; set; } = string.Empty;
    public string CoordinatesDescription { get; set; } = string.Empty;
    public List<string> DirectionsItems { get; set; } = [];
    public string MapButtonLabel { get; set; } = string.Empty;
}
