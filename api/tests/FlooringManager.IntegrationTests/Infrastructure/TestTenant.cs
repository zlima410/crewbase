using System.Net.Http.Headers;
using FlooringManager.Domain.Companies;
using FlooringManager.Domain.Customers;
using FlooringManager.Domain.Properties;
using FlooringManager.Domain.Shared;
using FlooringManager.Domain.Users;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FlooringManager.IntegrationTests;

/// <summary>A seeded company with one user, customer, and property.</summary>
public sealed record TestTenant(Guid CompanyId, Guid Sub, Guid CustomerId, Guid PropertyId);

public static class TestTenantExtensions
{
    /// <summary>
    /// Seeds a complete, isolated tenant. Every caller gets a fresh company so tests
    /// never share tenant state, which also keeps per-company numbering assertions
    /// independent of test execution order.
    /// </summary>
    public static async Task<TestTenant> SeedTenantAsync(this ApiFactoryBase factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;

        var company = new Company { Id = Guid.NewGuid(), Name = "Acme Floors", CreatedAt = now };
        var sub = Guid.NewGuid();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            FirstName = "Sarah",
            LastName = "Johnson",
            Email = "sarah@example.com",
            Phone = "555-1234",
            CreatedAt = now,
            UpdatedAt = now
        };
        var property = new Property
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            StreetAddress = "123 Oak St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62704",
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Companies.Add(company);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            AuthProviderUserId = sub,
            CompanyId = company.Id,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Role = UserRole.OfficeManager,
            IsActive = true,
            CreatedAt = now
        });
        db.Customers.Add(customer);
        db.Properties.Add(property);
        await db.SaveChangesAsync();

        return new TestTenant(company.Id, sub, customer.Id, property.Id);
    }

    public static HttpClient ClientFor(this ApiFactoryBase factory, Guid sub)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            new TestJwtBuilder(factory.SigningKey).WithSub(sub).Build());

        return client;
    }
}
