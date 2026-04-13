namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public class Season
{
    public int SeasonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PercentageChange { get; set; }
    public bool IsActive { get; set; }
}
