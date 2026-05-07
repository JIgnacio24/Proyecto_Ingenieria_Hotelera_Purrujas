namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public sealed class GalleryImage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Src { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Category { get; set; } = "hotel";
    public bool IsActive { get; set; } = true;
}