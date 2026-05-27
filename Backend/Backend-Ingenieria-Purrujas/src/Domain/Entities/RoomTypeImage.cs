namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public sealed class RoomTypeImage
{
    public int RoomTypeImageId { get; set; }
    public int RoomTypeId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
