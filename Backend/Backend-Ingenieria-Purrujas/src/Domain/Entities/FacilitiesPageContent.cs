namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public sealed class FacilitiesPageContent
{
    public string SectionTag { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public string HighlightTitle { get; set; } = string.Empty;
    public string HighlightDescription { get; set; } = string.Empty;
    public string PrimaryListTitle { get; set; } = string.Empty;
    public List<string> PrimaryListItems { get; set; } = [];
    public string SecondaryListTitle { get; set; } = string.Empty;
    public List<string> SecondaryListItems { get; set; } = [];
    public List<FacilitiesServiceText> ServiceCards { get; set; } = [];
}

public sealed class FacilitiesServiceText
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
