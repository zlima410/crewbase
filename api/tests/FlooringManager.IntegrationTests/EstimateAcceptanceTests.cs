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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlooringManager.IntegrationTests;

public sealed class EstimateAcceptanceTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Estimates = "/api/v1/estimates";

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
            Email = "sarah@example.com",
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

    private async Task<Estimate> SeedEstimateAsync(
        Guid companyId,
        Guid customerId,
        Guid propertyId,
        EstimateStatus status,
        string number = "EST-9000",
        bool withRoom = false)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;

        var estimate = new Estimate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CustomerId = customerId,
            PropertyId = propertyId,
            EstimateNumber = number,
            Status = status,
            CreatedDate = now,
            UpdatedAt = now,
            Notes = "sand in place"
        };

        if (withRoom)
        {
            estimate.Rooms.Add(new EstimateRoom
            {
                Id = Guid.NewGuid(),
                Name = "Living Room",
                LengthFeet = 20m,
                WidthFeet = 15m,
                WastePercentage = 10m,
                SquareFeet = 300m,
                BillableSquareFeet = 330m,
                FlooringType = FlooringType.SolidHardwood,
                WorkType = WorkType.NewInstallation,
                InstallationMethod = InstallationMethod.NailDown,
                FinishType = FinishType.OilBased,
                Notes = "site finished",
                LaborRatePerSqFt = 4m,
                MaterialRatePerSqFt = 1.25m,
                Position = 0
            });
        }

        db.Estimates.Add(estimate);
        await db.SaveChangesAsync();
        return estimate;
    }

    private HttpClient ClientFor(Guid sub)
    {
        var client = factory.CreateClient();
        var token = new TestJwtBuilder(factory.SigningKey).WithSub(sub).Build();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static EstimateRoomInput Room(
        string name = "Living Room",
        InstallationMethod? installation = InstallationMethod.NailDown,
        FinishType? finish = FinishType.OilBased,
        string? notes = "site finished") =>
        new(null, name, 20m, 15m, 10m,
            FlooringType.SolidHardwood, WorkType.NewInstallation, 4m, 1.25m,
            installation, finish, notes);

    private static CreateEstimateRequest CreateRequest(Guid customerId, Guid propertyId, params EstimateRoomInput[] rooms) =>
        new(customerId, propertyId, null, 0m, "sand in place", rooms.ToList());

    private async Task<EstimateResponse> CreateAndSendAsync(HttpClient client, Guid customerId, Guid propertyId)
    {
        var created = await client.PostAsJsonAsync(Estimates, CreateRequest(customerId, propertyId, Room()));
        created.EnsureSuccessStatusCode();
        var body = (await created.Content.ReadFromJsonAsync<EstimateResponse>(TestJson.Options))!;

        var sent = await client.PostAsync($"{Estimates}/{body.Id}/send", null);
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        return (await sent.Content.ReadFromJsonAsync<EstimateResponse>(TestJson.Options))!;
    }

    [Fact]
    public async Task Send_Draft_Returns200_WithSentStatus()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var created = await client.PostAsJsonAsync(Estimates, CreateRequest(customer.Id, property.Id, Room()));
        var body = await created.Content.ReadFromJsonAsync<EstimateResponse>(TestJson.Options);

        var response = await client.PostAsync($"{Estimates}/{body!.Id}/send", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sent = await response.Content.ReadFromJsonAsync<EstimateResponse>(TestJson.Options);
        Assert.Equal(EstimateStatus.Sent, sent!.Status);
    }

    [Fact]
    public async Task Send_AlreadySent_Returns200_Idempotent()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);
        var estimate = await CreateAndSendAsync(client, customer.Id, property.Id);

        var again = await client.PostAsync($"{Estimates}/{estimate.Id}/send", null);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        var body = await again.Content.ReadFromJsonAsync<EstimateResponse>(TestJson.Options);
        Assert.Equal(EstimateStatus.Sent, body!.Status);
    }

    [Theory]
    [InlineData(EstimateStatus.Accepted)]
    [InlineData(EstimateStatus.Rejected)]
    [InlineData(EstimateStatus.Expired)]
    public async Task Send_NonDraftNonSent_Returns404(EstimateStatus status)
    {
        var (companyId, sub, customer, property) = await SeedFullTenantAsync();
        var locked = await SeedEstimateAsync(companyId, customer.Id, property.Id, status);
        var client = ClientFor(sub);

        var response = await client.PostAsync($"{Estimates}/{locked.Id}/send", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Accept_SentEstimate_CreatesScheduledJob_AndCopiesRooms()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);
        var estimate = await CreateAndSendAsync(client, customer.Id, property.Id);

        var response = await client.PostAsync($"{Estimates}/{estimate.Id}/accept", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var job = await response.Content.ReadFromJsonAsync<JobResponse>(TestJson.Options);
        Assert.NotNull(job);
        Assert.Contains($"/api/v1/jobs/{job!.Id}",
            response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

        Assert.Equal(JobStatus.Scheduled, job.Status);
        Assert.Equal(estimate.Id, job.EstimateId);
        Assert.Equal(customer.Id, job.CustomerId);
        Assert.Equal($"{customer.FirstName} {customer.LastName}", job.CustomerName);
        Assert.Equal(customer.Phone, job.CustomerPhone);
        Assert.Equal(customer.Email, job.CustomerEmail);
        Assert.Equal(property.Id, job.PropertyId);
        Assert.Contains(property.StreetAddress, job.PropertyAddress);
        Assert.Null(job.PropertyAccessNotes);
        Assert.Equal("sand in place", job.Description);
        Assert.Null(job.InternalNotes);
        Assert.Null(job.ScheduledStart);
        Assert.StartsWith("JOB-", job.JobNumber);

        var room = Assert.Single(job.Rooms);
        Assert.Equal("Living Room", room.Name);
        Assert.Equal(300m, room.SquareFeet);
        Assert.Equal(330m, room.BillableSquareFeet);
        Assert.Equal(FlooringType.SolidHardwood, room.FlooringType);
        Assert.Equal(WorkType.NewInstallation, room.WorkType);
        Assert.Equal(InstallationMethod.NailDown, room.InstallationMethod);
        Assert.Equal(FinishType.OilBased, room.FinishType);
        Assert.Equal("site finished", room.Notes);
        Assert.Equal(0, room.Position);
        Assert.NotEqual(estimate.Rooms[0].Id, room.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persisted = await db.Estimates.FirstAsync(e => e.Id == estimate.Id);
        Assert.Equal(EstimateStatus.Accepted, persisted.Status);
        Assert.Equal(1, await db.Jobs.CountAsync(j => j.EstimateId == estimate.Id));
    }

    [Fact]
    public async Task Accept_Repeat_Returns200_WithSameJob()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);
        var estimate = await CreateAndSendAsync(client, customer.Id, property.Id);

        var first = await client.PostAsync($"{Estimates}/{estimate.Id}/accept", null);
        var created = await first.Content.ReadFromJsonAsync<JobResponse>(TestJson.Options);

        var second = await client.PostAsync($"{Estimates}/{estimate.Id}/accept", null);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var existing = await second.Content.ReadFromJsonAsync<JobResponse>(TestJson.Options);
        Assert.Equal(created!.Id, existing!.Id);
        Assert.Equal(created.JobNumber, existing.JobNumber);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.Jobs.CountAsync(j => j.EstimateId == estimate.Id));
    }

    [Fact]
    public async Task Accept_Draft_Returns409_AndCreatesNoJob()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var created = await client.PostAsJsonAsync(Estimates, CreateRequest(customer.Id, property.Id, Room()));
        var body = await created.Content.ReadFromJsonAsync<EstimateResponse>(TestJson.Options);

        var response = await client.PostAsync($"{Estimates}/{body!.Id}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Jobs.AnyAsync(j => j.EstimateId == body.Id));
        Assert.Equal(EstimateStatus.Draft, (await db.Estimates.FirstAsync(e => e.Id == body.Id)).Status);
    }

    [Theory]
    [InlineData(EstimateStatus.Rejected)]
    [InlineData(EstimateStatus.Expired)]
    public async Task Accept_RejectedOrExpired_Returns409(EstimateStatus status)
    {
        var (companyId, sub, customer, property) = await SeedFullTenantAsync();
        var estimate = await SeedEstimateAsync(companyId, customer.Id, property.Id, status, withRoom: true);
        var client = ClientFor(sub);

        var response = await client.PostAsync($"{Estimates}/{estimate.Id}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Accept_SentWithNoRooms_Returns409()
    {
        var (companyId, sub, customer, property) = await SeedFullTenantAsync();
        var estimate = await SeedEstimateAsync(companyId, customer.Id, property.Id, EstimateStatus.Sent);
        var client = ClientFor(sub);

        var response = await client.PostAsync($"{Estimates}/{estimate.Id}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Jobs.AnyAsync(j => j.EstimateId == estimate.Id));
        Assert.Equal(EstimateStatus.Sent, (await db.Estimates.FirstAsync(e => e.Id == estimate.Id)).Status);
    }

    [Fact]
    public async Task Accept_UnknownEstimate_Returns404()
    {
        var (_, sub, _, _) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var response = await client.PostAsync($"{Estimates}/{Guid.NewGuid()}/accept", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Accept_EstimateFromAnotherCompany_Returns404()
    {
        var (companyA, _, customerA, propertyA) = await SeedFullTenantAsync();
        var victim = await SeedEstimateAsync(
            companyA, customerA.Id, propertyA.Id, EstimateStatus.Sent, withRoom: true);

        var (_, subB, _, _) = await SeedFullTenantAsync();
        var clientB = ClientFor(subB);

        var response = await clientB.PostAsync($"{Estimates}/{victim.Id}/accept", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Jobs.AnyAsync(j => j.EstimateId == victim.Id));
        Assert.Equal(EstimateStatus.Sent, (await db.Estimates.FirstAsync(e => e.Id == victim.Id)).Status);
    }

    [Fact]
    public async Task Accept_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient()
            .PostAsync($"{Estimates}/{Guid.NewGuid()}/accept", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task JobNumbers_AreSequential_PerCompany()
    {
        var (_, subA, customerA, propertyA) = await SeedFullTenantAsync();
        var (_, subB, customerB, propertyB) = await SeedFullTenantAsync();
        var clientA = ClientFor(subA);
        var clientB = ClientFor(subB);

        var a1 = await Accept(clientA, customerA.Id, propertyA.Id);
        var a2 = await Accept(clientA, customerA.Id, propertyA.Id);
        var b1 = await Accept(clientB, customerB.Id, propertyB.Id);

        Assert.Equal("JOB-0001", a1.JobNumber);
        Assert.Equal("JOB-0002", a2.JobNumber);
        Assert.Equal("JOB-0001", b1.JobNumber);
    }

    private async Task<JobResponse> Accept(HttpClient client, Guid customerId, Guid propertyId)
    {
        var estimate = await CreateAndSendAsync(client, customerId, propertyId);
        var response = await client.PostAsync($"{Estimates}/{estimate.Id}/accept", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JobResponse>(TestJson.Options))!;
    }
}
