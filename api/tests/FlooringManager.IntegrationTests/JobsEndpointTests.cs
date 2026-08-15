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
            AccessNotes = "Gate code 1234",
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
        Assert.Equal(customer.Id, body.CustomerId);
        Assert.Equal("Sarah Johnson", body.CustomerName);
        Assert.Equal("555-1234", body.CustomerPhone);
        Assert.Equal("sarah@example.com", body.CustomerEmail);
        Assert.Equal(property.Id, body.PropertyId);
        Assert.Contains("123 Oak Lane", body.PropertyAddress);
        Assert.Equal("Gate code 1234", body.PropertyAccessNotes);
        Assert.Null(body.ScheduledStart);
        Assert.Null(body.ActualStart);

        var room = Assert.Single(body.Rooms);
        Assert.Equal("Living Room", room.Name);
        Assert.Equal(100m, room.SquareFeet);
        Assert.Equal(100m, room.BillableSquareFeet);
        Assert.Equal(FlooringType.SolidHardwood, room.FlooringType);
        Assert.Equal(WorkType.NewInstallation, room.WorkType);
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

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/jobs");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_Empty_ReturnsEmptyPage()
    {
        var (_, sub, _, _) = await SeedFullTenantAsync();
        var list = await ClientFor(sub).GetFromJsonAsync<JobListResponse>("/api/v1/jobs", TestJson.Options);

        Assert.NotNull(list);
        Assert.Equal(0, list.Total);
        Assert.Empty(list.Items);
        Assert.Equal(1, list.Page);
        Assert.Equal(25, list.PageSize);
    }

    [Fact]
    public async Task List_ScopedToCompany()
    {
        var (companyA, subA, customerA, propertyA) = await SeedFullTenantAsync();
        for (var i = 0; i < 3; i++)
            await SeedJobAsync(companyA, customerA.Id, propertyA.Id, number: $"JOB-A-{i:D4}");

        var (companyB, subB, customerB, propertyB) = await SeedFullTenantAsync();
        for (var i = 0; i < 2; i++)
            await SeedJobAsync(companyB, customerB.Id, propertyB.Id, number: $"JOB-B-{i:D4}");

        var listA = await ClientFor(subA).GetFromJsonAsync<JobListResponse>("/api/v1/jobs", TestJson.Options);
        var listB = await ClientFor(subB).GetFromJsonAsync<JobListResponse>("/api/v1/jobs", TestJson.Options);

        Assert.Equal(3, listA!.Total);
        Assert.All(listA.Items, i => Assert.StartsWith("JOB-A-", i.JobNumber));

        Assert.Equal(2, listB!.Total);
        Assert.All(listB.Items, i => Assert.StartsWith("JOB-B-", i.JobNumber));
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        var (companyId, sub, customer, property) = await SeedFullTenantAsync();
        await SeedJobAsync(companyId, customer.Id, property.Id,
            status: JobStatus.Scheduled, number: "JOB-S-1");
        await SeedJobAsync(companyId, customer.Id, property.Id,
            status: JobStatus.Scheduled, number: "JOB-S-2");
        await SeedJobAsync(companyId, customer.Id, property.Id,
            status: JobStatus.InProgress, number: "JOB-P-1");
        await SeedJobAsync(companyId, customer.Id, property.Id,
            status: JobStatus.Completed, number: "JOB-C-1");

        var client = ClientFor(sub);

        var scheduled = await client.GetFromJsonAsync<JobListResponse>(
            $"/api/v1/jobs?status={JobStatus.Scheduled}", TestJson.Options);
        Assert.Equal(2, scheduled!.Total);
        Assert.All(scheduled.Items, i => Assert.Equal(JobStatus.Scheduled, i.Status));

        var inProgress = await client.GetFromJsonAsync<JobListResponse>(
            $"/api/v1/jobs?status={JobStatus.InProgress}", TestJson.Options);
        Assert.Equal(1, inProgress!.Total);
        Assert.Equal("JOB-P-1", inProgress.Items[0].JobNumber);

        var completed = await client.GetFromJsonAsync<JobListResponse>(
            $"/api/v1/jobs?status={JobStatus.Completed}", TestJson.Options);
        Assert.Equal(1, completed!.Total);
        Assert.Equal("JOB-C-1", completed.Items[0].JobNumber);
    }

    [Fact]
    public async Task List_FiltersBySearch_JobNumberCustomerAddress()
    {
        var (companyId, sub, johnson, oak) = await SeedFullTenantAsync();
        await SeedJobAsync(companyId, johnson.Id, oak.Id, number: "JOB-1001");

        var miller = await SeedCustomerAsync(companyId, "Mike", "Miller");
        var pine = await SeedPropertyAsync(miller.Id, "456 Pine Street");
        await SeedJobAsync(companyId, miller.Id, pine.Id, number: "JOB-1002");

        var client = ClientFor(sub);

        var byNumber = await client.GetFromJsonAsync<JobListResponse>(
            "/api/v1/jobs?search=JOB-1001", TestJson.Options);
        Assert.Equal(1, byNumber!.Total);
        Assert.Equal("JOB-1001", byNumber.Items[0].JobNumber);

        var byCustomer = await client.GetFromJsonAsync<JobListResponse>(
            "/api/v1/jobs?search=miller", TestJson.Options);
        Assert.Equal(1, byCustomer!.Total);
        Assert.Equal("Mike Miller", byCustomer.Items[0].CustomerName);

        var byAddress = await client.GetFromJsonAsync<JobListResponse>(
            "/api/v1/jobs?search=Oak", TestJson.Options);
        Assert.Equal(1, byAddress!.Total);
        Assert.Contains("Oak Lane", byAddress.Items[0].PropertyAddress);
    }

    [Fact]
    public async Task List_Paginates_AndOrdersScheduledBeforeCompleted()
    {
        var (companyId, sub, customer, property) = await SeedFullTenantAsync();
        var now = DateTimeOffset.UtcNow;

        await SeedJobAsync(companyId, customer.Id, property.Id,
            status: JobStatus.Completed, number: "JOB-OLD-C", createdAt: now.AddDays(-1));
        await SeedJobAsync(companyId, customer.Id, property.Id,
            status: JobStatus.Scheduled, number: "JOB-NEW-S", createdAt: now);

        var seededNumbers = new List<string>();
        for (var i = 0; i < 15; i++)
        {
            var number = $"JOB-P-{i:D4}";
            await SeedJobAsync(companyId, customer.Id, property.Id,
                status: JobStatus.Scheduled,
                number: number,
                createdAt: now.AddMinutes(i));
            seededNumbers.Add(number);
        }

        var client = ClientFor(sub);
        var unfiltered = await client.GetFromJsonAsync<JobListResponse>(
            "/api/v1/jobs?page=1&pageSize=5", TestJson.Options);
        Assert.Equal(JobStatus.Scheduled, unfiltered!.Items[0].Status);
        Assert.DoesNotContain(unfiltered.Items, i => i.Status == JobStatus.Completed);

        var page1 = await client.GetFromJsonAsync<JobListResponse>(
            $"/api/v1/jobs?status={JobStatus.Scheduled}&page=1&pageSize=10", TestJson.Options);
        var page2 = await client.GetFromJsonAsync<JobListResponse>(
            $"/api/v1/jobs?status={JobStatus.Scheduled}&page=2&pageSize=10", TestJson.Options);

        Assert.Equal(16, page1!.Total);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(6, page2!.Items.Count);
        Assert.Equal(seededNumbers[^1], page1.Items.First().JobNumber);

        var ids1 = page1.Items.Select(i => i.Id).ToHashSet();
        var ids2 = page2.Items.Select(i => i.Id).ToHashSet();
        Assert.Empty(ids1.Intersect(ids2));
    }

    private async Task<Customer> SeedCustomerAsync(
        Guid companyId,
        string firstName,
        string lastName)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FirstName = firstName,
            LastName = lastName,
            Phone = "555-0000",
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private async Task<Property> SeedPropertyAsync(Guid customerId, string streetAddress)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        var property = new Property
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            StreetAddress = streetAddress,
            City = "Portland",
            State = "OR",
            PostalCode = "97205",
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Properties.Add(property);
        await db.SaveChangesAsync();
        return property;
    }

    private async Task SeedJobAsync(
        Guid companyId,
        Guid customerId,
        Guid propertyId,
        JobStatus status = JobStatus.Scheduled,
        string number = "JOB-9000",
        DateTimeOffset? createdAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = createdAt ?? DateTimeOffset.UtcNow;

        var estimate = new Estimate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CustomerId = customerId,
            PropertyId = propertyId,
            EstimateNumber = $"EST-FOR-{number}",
            Status = EstimateStatus.Accepted,
            CreatedDate = now,
            UpdatedAt = now
        };
        db.Estimates.Add(estimate);
        db.Jobs.Add(new Job
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CustomerId = customerId,
            PropertyId = propertyId,
            EstimateId = estimate.Id,
            JobNumber = number,
            Status = status,
            CreatedAt = now
        });
        await db.SaveChangesAsync();
    }
}
