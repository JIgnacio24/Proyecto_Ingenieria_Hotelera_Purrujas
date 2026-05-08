using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Customer> CreateAsync(string name, string lastName, string email, string? phone, string creditCard, CancellationToken cancellationToken = default);
}
