using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlooringManager.Application.Estimates;
using FlooringManager.Application.Jobs;
using FlooringManager.Domain.Companies;
using FlooringManager.Domain.Customers;
using FlooringManager.Domain.Estimates;
using FlooringManager.Domain.Jobs;
using FlooringManager.Domain.Properties;
using FlooringManager.Domain.Shared;
using FlooringManager.Domain.Users;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FlooringManager.IntegrationTests;

public sealed class JobsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<(Guid companyId, Guid sub, Customer customer, Property property)>
        SeedFullTenantAsync()
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
            Phone = "555-1234",
            CreatedAt = now,
            UpdatedAt = now
        };
        var property = new Property
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            StreetAddress = "123 Oak Lane",
            City = "Portland",
            State = "OR",
            PostalCode = "97205",
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
        return (company.Id, sub, customer, property);
    }

    private HttpClient ClientFor(Guid sub)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            new TestJwtBuilder(factory.SigningKey).WithSub(sub).Build());
        return client;
    }

    private async Task<JobResponse> AcceptJobAsync(HttpClient client, Guid customerId, Guid propertyId)
    {
        var created = await client.PostAsJsonAsync("/api/v1/estimates",
            new CreateEstimateRequest(customerId, propertyId, null, 0m, null,
                [new EstimateRoomInput(null, "Living Room", 10m, 10m, 0m,
                    FlooringType.SolidHardwood, WorkType.NewInstallation, 3m, 1m)]));
        var estimate = await created.Content.ReadFromJsonAsync<EstimateResponse>(TestJson.Options);
        await client.PostAsync($"/api/v1/estimates/{estimate!.Id}/send", null);

        var accepted = await client.PostAsync($"/api/v1/estimates/{estimate.Id}/accept", null);
        accepted.EnsureSuccessStatusCode();
        return (await accepted.Content.ReadFromJsonAsync<JobResponse>(TestJson.Options))!;
    }

    [Fact]
    public async Task Get_ExistingJob_Returns200()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);
        var created = await AcceptJobAsync(client, customer.Id, property.Id);

        var response = await client.GetAsync($"/api/v1/jobs/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JobResponse>(TestJson.Options);
        Assert.Equal(created.Id, body!.Id);
        Assert.Equal(created.JobNumber, body.JobNumber);
        Assert.Equal(JobStatus.Scheduled, body.Status);
        Assert.Single(body.Rooms);
    }

    [Fact]
    public async Task Get_UnknownJob_Returns404()
    {
        var (_, sub, _, _) = await SeedFullTenantAsync();
        var response = await ClientFor(sub).GetAsync($"/api/v1/jobs/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_JobFromAnotherCompany_Returns404()
    {
        var (_, subA, customerA, propertyA) = await SeedFullTenantAsync();
        var job = await AcceptJobAsync(ClientFor(subA), customerA.Id, propertyA.Id);

        var (_, subB, _, _) = await SeedFullTenantAsync();
        var response = await ClientFor(subB).GetAsync($"/api/v1/jobs/{job.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync($"/api/v1/jobs/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
