using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlooringManager.Application.Customers;
using FlooringManager.Domain.Companies;
using FlooringManager.Domain.Customers;
using FlooringManager.Domain.Shared;
using FlooringManager.Domain.Users;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FlooringManager.IntegrationTests;

public sealed class CustomersEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Endpoint = "/api/v1/customers";

    private async Task<(Guid companyId, Guid sub)> SeedCompanyAndUserAsync(
        UserRole role = UserRole.OfficeManager)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme Floors",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var sub = Guid.NewGuid();
        db.Companies.Add(company);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            AuthProviderUserId = sub,
            CompanyId = company.Id,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Role = role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return (company.Id, sub);
    }

    private async Task<Customer> SeedCustomerAsync(
        Guid companyId,
        string firstName = "John",
        string lastName = "Doe",
        string? email = "john@example.com",
        string phone = "555-1234",
        string? notes = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private HttpClient ClientFor(Guid sub)
    {
        var client = factory.CreateClient();
        var token = new TestJwtBuilder().WithSub(sub).Build();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient AnonymousClient() => factory.CreateClient();

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        var body = new CreateCustomerRequest("Ann", "Smith", null, "555-0100", null);

        var response = await AnonymousClient().PostAsJsonAsync(Endpoint, body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Valid_Returns201WithBody()
    {
        var (_, sub) = await SeedCompanyAndUserAsync();
        var client = ClientFor(sub);

        var request = new CreateCustomerRequest(
            FirstName: "Sarah",
            LastName: "Johnson",
            Email: "sarah@example.com",
            Phone: "555-9000",
            Notes: "Referred by neighbor");

        var response = await client.PostAsJsonAsync(Endpoint, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal("Sarah", body.FirstName);
        Assert.Equal("Johnson", body.LastName);
        Assert.Equal("sarah@example.com", body.Email);
        Assert.Equal("555-9000", body.Phone);
        Assert.Equal("Referred by neighbor", body.Notes);

        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/api/v1/customers/{body.Id}",
            response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "Smith", "555-1000")]
    [InlineData("Ann", "", "555-1000")]
    [InlineData("Ann", "Smith", "")]
    public async Task Post_MissingRequiredFields_Returns400(
        string firstName, string lastName, string phone)
    {
        var (_, sub) = await SeedCompanyAndUserAsync();
        var client = ClientFor(sub);

        var request = new CreateCustomerRequest(firstName, lastName, null, phone, null);

        var response = await client.PostAsJsonAsync(Endpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidEmail_Returns400()
    {
        var (_, sub) = await SeedCompanyAndUserAsync();
        var client = ClientFor(sub);

        var request = new CreateCustomerRequest(
            "Ann", "Smith", "not-an-email", "555-0100", null);

        var response = await client.PostAsJsonAsync(Endpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ExistingCustomer_Returns200()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var seeded = await SeedCustomerAsync(companyId,
            firstName: "Lee", lastName: "Nguyen",
            email: "lee@example.com", phone: "555-2222", notes: "VIP");

        var client = ClientFor(sub);
        var response = await client.GetAsync($"{Endpoint}/{seeded.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(body);
        Assert.Equal(seeded.Id, body!.Id);
        Assert.Equal("Lee", body.FirstName);
        Assert.Equal("Nguyen", body.LastName);
        Assert.Equal("lee@example.com", body.Email);
        Assert.Equal("555-2222", body.Phone);
        Assert.Equal("VIP", body.Notes);
    }

    [Fact]
    public async Task Get_NotFound_Returns404()
    {
        var (_, sub) = await SeedCompanyAndUserAsync();
        var client = ClientFor(sub);

        var response = await client.GetAsync($"{Endpoint}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_OtherCompanyCustomer_Returns404()
    {
        var (companyA, _) = await SeedCompanyAndUserAsync();
        var otherCustomer = await SeedCustomerAsync(companyA,
            firstName: "Private", lastName: "Record");

        var (_, subB) = await SeedCompanyAndUserAsync();
        var clientB = ClientFor(subB);

        var response = await clientB.GetAsync($"{Endpoint}/{otherCustomer.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Valid_Returns200AndPersists()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var seeded = await SeedCustomerAsync(companyId,
            firstName: "Old", lastName: "Name",
            email: "old@example.com", phone: "555-0000", notes: null);

        var client = ClientFor(sub);
        var update = new UpdateCustomerRequest(
            FirstName: "New",
            LastName: "Name",
            Email: "new@example.com",
            Phone: "555-1111",
            Notes: "Updated notes");

        var putResponse = await client.PutAsJsonAsync($"{Endpoint}/{seeded.Id}", update);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(updated);
        Assert.Equal("New", updated!.FirstName);
        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal("555-1111", updated.Phone);
        Assert.Equal("Updated notes", updated.Notes);
        Assert.True(updated.UpdatedAt > seeded.UpdatedAt,
            "UpdatedAt should advance after an update.");
        Assert.Equal(seeded.CreatedAt.ToUnixTimeSeconds(),
            updated.CreatedAt.ToUnixTimeSeconds());

        var getResponse = await client.GetAsync($"{Endpoint}/{seeded.Id}");
        var afterGet = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.Equal("New", afterGet!.FirstName);
        Assert.Equal("555-1111", afterGet.Phone);
    }

    [Fact]
    public async Task Put_OtherCompanyCustomer_Returns404()
    {
        var (companyA, _) = await SeedCompanyAndUserAsync();
        var victim = await SeedCustomerAsync(companyA,
            firstName: "DoNot", lastName: "Touch",
            email: "victim@example.com", phone: "555-9999", notes: "Sensitive");

        var (_, subB) = await SeedCompanyAndUserAsync();
        var clientB = ClientFor(subB);

        var update = new UpdateCustomerRequest(
            "Hacked", "Name", "hacker@example.com", "555-0000", "pwn3d");

        var response = await clientB.PutAsJsonAsync($"{Endpoint}/{victim.Id}", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var reread = await db.Customers.FindAsync(victim.Id);
        Assert.NotNull(reread);
        Assert.Equal("DoNot", reread!.FirstName);
        Assert.Equal("victim@example.com", reread.Email);
        Assert.Equal("Sensitive", reread.Notes);
    }

    [Fact]
    public async Task Get_Search_MatchesByNameEmailAndPhone()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        await SeedCustomerAsync(companyId, "Johnathan", "Miller", "jm@example.com", "555-1000");
        await SeedCustomerAsync(companyId, "Alice",     "Johnston", "alice@example.com", "555-2000");
        await SeedCustomerAsync(companyId, "Bob",       "Baker",   "bob@floors.io",    "555-3000");
        await SeedCustomerAsync(companyId, "Carol",     "Davis",   "carol@example.com", "555-JOHN");

        var client = ClientFor(sub);

        var byFirst = await client.GetFromJsonAsync<CustomerListResponse>($"{Endpoint}?search=JOHN");
        Assert.NotNull(byFirst);
        var firstNames = byFirst!.Items.Select(i => i.FirstName).ToHashSet();
        Assert.Contains("Johnathan", firstNames);
        Assert.Contains("Alice",     firstNames);
        Assert.Contains("Carol",     firstNames);
        Assert.DoesNotContain("Bob", firstNames);

        var byEmail = await client.GetFromJsonAsync<CustomerListResponse>($"{Endpoint}?search=floors.io");
        Assert.Single(byEmail!.Items);
        Assert.Equal("Bob", byEmail.Items[0].FirstName);
    }

    [Fact]
    public async Task Get_Search_Paginates()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();

        for (var i = 0; i < 30; i++)
        {
            await SeedCustomerAsync(companyId,
                firstName: "First",
                lastName:  $"Last{i:D2}",
                email:     $"user{i}@example.com",
                phone:     $"555-{i:D4}");
        }

        var client = ClientFor(sub);

        var page1 = await client.GetFromJsonAsync<CustomerListResponse>(
            $"{Endpoint}?page=1&pageSize=10");
        var page2 = await client.GetFromJsonAsync<CustomerListResponse>(
            $"{Endpoint}?page=2&pageSize=10");
        var page3 = await client.GetFromJsonAsync<CustomerListResponse>(
            $"{Endpoint}?page=3&pageSize=10");

        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.NotNull(page3);

        Assert.Equal(30, page1!.Total);
        Assert.Equal(30, page2!.Total);
        Assert.Equal(30, page3!.Total);

        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(10, page2.Items.Count);
        Assert.Equal(10, page3.Items.Count);

        Assert.Equal("Last00", page1.Items.First().LastName);
        Assert.Equal("Last09", page1.Items.Last().LastName);
        Assert.Equal("Last10", page2.Items.First().LastName);
        Assert.Equal("Last19", page2.Items.Last().LastName);
        Assert.Equal("Last20", page3.Items.First().LastName);
        Assert.Equal("Last29", page3.Items.Last().LastName);

        var ids1 = page1.Items.Select(i => i.Id).ToHashSet();
        var ids2 = page2.Items.Select(i => i.Id).ToHashSet();
        Assert.Empty(ids1.Intersect(ids2));
    }

    [Fact]
    public async Task Get_Search_ScopedToCompany()
    {
        var (companyA, subA) = await SeedCompanyAndUserAsync();
        var (companyB, subB) = await SeedCompanyAndUserAsync();

        await SeedCustomerAsync(companyA, "Ada",    "Alpha", "ada@a.example", "555-0001");
        await SeedCustomerAsync(companyA, "Alan",   "Alpha", "alan@a.example", "555-0002");

        for (var i = 0; i < 5; i++)
        {
            await SeedCustomerAsync(companyB,
                firstName: "Beth",
                lastName:  $"Beta{i}",
                email:     $"beth{i}@b.example",
                phone:     $"555-1{i:D3}");
        }

        var clientA = ClientFor(subA);
        var clientB = ClientFor(subB);

        var listA = await clientA.GetFromJsonAsync<CustomerListResponse>(Endpoint);
        var listB = await clientB.GetFromJsonAsync<CustomerListResponse>(Endpoint);

        Assert.Equal(2, listA!.Total);
        Assert.All(listA.Items, c => Assert.Equal("Alpha", c.LastName));

        Assert.Equal(5, listB!.Total);
        Assert.All(listB.Items, c => Assert.Equal("Beth", c.FirstName));

        var crossSearch = await clientA.GetFromJsonAsync<CustomerListResponse>(
            $"{Endpoint}?search=Beth");
        Assert.Equal(0, crossSearch!.Total);
        Assert.Empty(crossSearch.Items);
    }

    [Fact]
    public async Task Post_UnprovisionedUser_Returns403()
    {
        var client = ClientFor(Guid.NewGuid());

        var request = new CreateCustomerRequest(
            "Ghost", "User", null, "555-0000", null);

        var response = await client.PostAsJsonAsync(Endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}