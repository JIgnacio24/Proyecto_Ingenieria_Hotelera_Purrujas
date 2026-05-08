using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly string _connectionString;

    public CustomerRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Customer_GetByEmail", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Email", email);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Customer
            {
                CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                Name       = reader.GetString(reader.GetOrdinal("Name")),
                LastName   = reader.GetString(reader.GetOrdinal("LastName")),
                Email      = reader.GetString(reader.GetOrdinal("Email")),
                Phone      = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                CreditCard = reader.GetString(reader.GetOrdinal("CreditCard")),
                IsActive   = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            };
        }
        return null;
    }

    public async Task<Customer> CreateAsync(string name, string lastName, string email, string? phone, string creditCard, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Customer_Create", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Name", name);
        cmd.Parameters.AddWithValue("@LastName", lastName);
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@Phone", (object?)phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreditCard", creditCard);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);

        return new Customer
        {
            CustomerId = Convert.ToInt32(scalar),
            Name       = name,
            LastName   = lastName,
            Email      = email,
            Phone      = phone,
            CreditCard = creditCard,
            IsActive   = true
        };
    }
}
