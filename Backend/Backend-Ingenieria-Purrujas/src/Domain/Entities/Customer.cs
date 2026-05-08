namespace Backend_Ingenieria_Purrujas.Domain.Entities;

public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string CreditCard { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
