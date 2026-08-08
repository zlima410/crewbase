using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlooringManager.Application.Properties;
using FlooringManager.Domain.Companies;
using FlooringManager.Domain.Customers;
using FlooringManager.Domain.Properties;
using FlooringManager.Domain.Shared;
using FlooringManager.Domain.Users;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FlooringManager.IntegrationTests;

public sealed class PropertiesEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string PropertiesForCustomer(Guid customerId) =>
        $"/api/v1/customers/{customerId}/properties";

    private static string PropertyById(Guid propertyId) =>
        $"/api/v1/properties/{propertyId}";

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
        string lastName = "Doe")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FirstName = firstName,
            LastName = lastName,
            Email = "john@example.com",
            Phone = "555-1234",
            Notes = null,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private async Task<Property> SeedPropertyAsync(
        Guid customerId,
        string streetAddress = "123 Oak St",
        string city = "Portland",
        string state = "OR",
        string postalCode = "97201",
        string? accessNotes = null,
        DateTimeOffset? createdAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = createdAt ?? DateTimeOffset.UtcNow.AddMinutes(-5);
        var property = new Property
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            StreetAddress = streetAddress,
            City = city,
            State = state,
            PostalCode = postalCode,
            AccessNotes = accessNotes,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Properties.Add(property);
        await db.SaveChangesAsync();
        return property;
    }

    private HttpClient ClientFor(Guid sub)
    {
        var client = factory.CreateClient();
        var token = new TestJwtBuilder(factory.SigningKey).WithSub(sub).Build();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient AnonymousClient() => factory.CreateClient();

    private static CreatePropertyRequest ValidCreate(
        string street = "500 Elm St",
        string city = "Portland",
        string state = "OR",
        string postal = "97202",
        string? access = null) =>
        new(street, city, state, postal, access);

    private static UpdatePropertyRequest ValidUpdate(
        string street = "500 Elm St",
        string city = "Portland",
        string state = "OR",
        string postal = "97202",
        string? access = null) =>
        new(street, city, state, postal, access);

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        var response = await AnonymousClient()
            .PostAsJsonAsync(PropertiesForCustomer(Guid.NewGuid()), ValidCreate());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        var response = await AnonymousClient()
            .GetAsync(PropertiesForCustomer(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        var response = await AnonymousClient()
            .GetAsync(PropertyById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_WithoutToken_Returns401()
    {
        var response = await AnonymousClient()
            .PutAsJsonAsync(PropertyById(Guid.NewGuid()), ValidUpdate());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnprovisionedUser_Returns403()
    {
        var client = ClientFor(Guid.NewGuid());

        var response = await client
            .PostAsJsonAsync(PropertiesForCustomer(Guid.NewGuid()), ValidCreate());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Put_UnprovisionedUser_Returns403()
    {
        var client = ClientFor(Guid.NewGuid());

        var response = await client
            .PutAsJsonAsync(PropertyById(Guid.NewGuid()), ValidUpdate());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Valid_Returns201WithBody()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer = await SeedCustomerAsync(companyId);
        var client = ClientFor(sub);

        var request = new CreatePropertyRequest(
            StreetAddress: "742 Evergreen Terrace",
            City: "Springfield",
            State: "IL",
            PostalCode: "62701",
            AccessNotes: "Gate code 1234; friendly dog");

        var response = await client
            .PostAsJsonAsync(PropertiesForCustomer(customer.Id), request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal(customer.Id, body.CustomerId);
        Assert.Equal("742 Evergreen Terrace", body.StreetAddress);
        Assert.Equal("Springfield", body.City);
        Assert.Equal("IL", body.State);
        Assert.Equal("62701", body.PostalCode);
        Assert.Equal("Gate code 1234; friendly dog", body.AccessNotes);
        Assert.Equal(body.CreatedAt.ToUnixTimeSeconds(),
            body.UpdatedAt.ToUnixTimeSeconds());

        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/api/v1/properties/{body.Id}",
            response.Headers.Location!.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_TrimsWhitespaceAndNormalizesEmptyAccessNotes()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer = await SeedCustomerAsync(companyId);
        var client = ClientFor(sub);

        var request = new CreatePropertyRequest(
            StreetAddress: "  200 Main St  ",
            City: "  Portland  ",
            State: " OR ",
            PostalCode: "  97201 ",
            AccessNotes: "   ");

        var response = await client
            .PostAsJsonAsync(PropertiesForCustomer(customer.Id), request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.Equal("200 Main St", body!.StreetAddress);
        Assert.Equal("Portland", body.City);
        Assert.Equal("OR", body.State);
        Assert.Equal("97201", body.PostalCode);
        Assert.Null(body.AccessNotes);
    }

    [Fact]
    public async Task Post_UnknownCustomer_Returns404()
    {
        var (_, sub) = await SeedCompanyAndUserAsync();
        var client = ClientFor(sub);

        var response = await client
            .PostAsJsonAsync(PropertiesForCustomer(Guid.NewGuid()), ValidCreate());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CustomerFromAnotherCompany_Returns404()
    {
        var (companyA, _) = await SeedCompanyAndUserAsync();
        var otherCustomer = await SeedCustomerAsync(companyA);

        var (_, subB) = await SeedCompanyAndUserAsync();
        var clientB = ClientFor(subB);

        var response = await clientB
            .PostAsJsonAsync(PropertiesForCustomer(otherCustomer.Id), ValidCreate());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var leaked = db.Properties.Any(p => p.CustomerId == otherCustomer.Id);
        Assert.False(leaked, "No property should have been created for the victim customer.");
    }

    [Theory]
    [InlineData("", "Portland", "OR", "97201")]
    [InlineData("100 Elm St", "", "OR", "97201")]
    [InlineData("100 Elm St", "Portland", "", "97201")]
    [InlineData("100 Elm St", "Portland", "OR", "")]
    public async Task Post_MissingRequiredFields_Returns400(
        string street, string city, string state, string postal)
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer = await SeedCustomerAsync(companyId);
        var client = ClientFor(sub);

        var request = new CreatePropertyRequest(street, city, state, postal, null);

        var response = await client
            .PostAsJsonAsync(PropertiesForCustomer(customer.Id), request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_TooLongFields_Returns400()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer = await SeedCustomerAsync(companyId);
        var client = ClientFor(sub);

        var request = new CreatePropertyRequest(
            StreetAddress: new string('a', 301),
            City: "Portland",
            State: "OR",
            PostalCode: "97201",
            AccessNotes: null);

        var response = await client
            .PostAsJsonAsync(PropertiesForCustomer(customer.Id), request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsAllPropertiesForCustomer_OrderedByCreatedAt()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer = await SeedCustomerAsync(companyId);

        var older = await SeedPropertyAsync(customer.Id,
            streetAddress: "1 Old Way",
            createdAt: DateTimeOffset.UtcNow.AddDays(-10));
        var middle = await SeedPropertyAsync(customer.Id,
            streetAddress: "2 Mid Way",
            createdAt: DateTimeOffset.UtcNow.AddDays(-5));
        var newest = await SeedPropertyAsync(customer.Id,
            streetAddress: "3 New Way",
            createdAt: DateTimeOffset.UtcNow.AddHours(-1));

        var client = ClientFor(sub);
        var list = await client.GetFromJsonAsync<List<PropertyResponse>>(
            PropertiesForCustomer(customer.Id));

        Assert.NotNull(list);
        Assert.Equal(3, list!.Count);
        Assert.Equal(older.Id, list[0].Id);
        Assert.Equal(middle.Id, list[1].Id);
        Assert.Equal(newest.Id, list[2].Id);
    }

    [Fact]
    public async Task List_ReturnsEmpty_ForCustomerWithNoProperties()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer = await SeedCustomerAsync(companyId);

        var client = ClientFor(sub);
        var list = await client.GetFromJsonAsync<List<PropertyResponse>>(
            PropertiesForCustomer(customer.Id));

        Assert.NotNull(list);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task List_ReturnsEmpty_ForUnknownCustomer()
    {
        var (_, sub) = await SeedCompanyAndUserAsync();

        var client = ClientFor(sub);
        var list = await client.GetFromJsonAsync<List<PropertyResponse>>(
            PropertiesForCustomer(Guid.NewGuid()));

        Assert.NotNull(list);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task List_ReturnsEmpty_ForCustomerFromAnotherCompany()
    {
        var (companyA, _) = await SeedCompanyAndUserAsync();
        var otherCustomer = await SeedCustomerAsync(companyA);
        await SeedPropertyAsync(otherCustomer.Id, streetAddress: "Secret 1");
        await SeedPropertyAsync(otherCustomer.Id, streetAddress: "Secret 2");

        var (_, subB) = await SeedCompanyAndUserAsync();
        var clientB = ClientFor(subB);

        var list = await clientB.GetFromJsonAsync<List<PropertyResponse>>(
            PropertiesForCustomer(otherCustomer.Id));

        Assert.NotNull(list);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task List_OnlyIncludesPropertiesOfSpecifiedCustomer()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer1 = await SeedCustomerAsync(companyId, "Alpha", "One");
        var customer2 = await SeedCustomerAsync(companyId, "Bravo", "Two");

        var c1p1 = await SeedPropertyAsync(customer1.Id, streetAddress: "C1-P1");
        var c1p2 = await SeedPropertyAsync(customer1.Id, streetAddress: "C1-P2");
        await SeedPropertyAsync(customer2.Id, streetAddress: "C2-P1");
        await SeedPropertyAsync(customer2.Id, streetAddress: "C2-P2");
        await SeedPropertyAsync(customer2.Id, streetAddress: "C2-P3");

        var client = ClientFor(sub);
        var list = await client.GetFromJsonAsync<List<PropertyResponse>>(
            PropertiesForCustomer(customer1.Id));

        Assert.NotNull(list);
        Assert.Equal(2, list!.Count);
        var ids = list.Select(p => p.Id).ToHashSet();
        Assert.Contains(c1p1.Id, ids);
        Assert.Contains(c1p2.Id, ids);
        Assert.All(list, p => Assert.Equal(customer1.Id, p.CustomerId));
    }

    [Fact]
    public async Task GetById_Existing_Returns200()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer = await SeedCustomerAsync(companyId);
        var property = await SeedPropertyAsync(customer.Id,
            streetAddress: "9 Cedar Ln",
            city: "Bend",
            state: "OR",
            postalCode: "97701",
            accessNotes: "Side gate unlocked");

        var client = ClientFor(sub);
        var response = await client.GetAsync(PropertyById(property.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.NotNull(body);
        Assert.Equal(property.Id, body!.Id);
        Assert.Equal(customer.Id, body.CustomerId);
        Assert.Equal("9 Cedar Ln", body.StreetAddress);
        Assert.Equal("Bend", body.City);
        Assert.Equal("OR", body.State);
        Assert.Equal("97701", body.PostalCode);
        Assert.Equal("Side gate unlocked", body.AccessNotes);
    }

    [Fact]
    public async Task GetById_Unknown_Returns404()
    {
        var (_, sub) = await SeedCompanyAndUserAsync();
        var client = ClientFor(sub);

        var response = await client.GetAsync(PropertyById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_OtherCompanyProperty_Returns404()
    {
        var (companyA, _) = await SeedCompanyAndUserAsync();
        var otherCustomer = await SeedCustomerAsync(companyA);
        var otherProperty = await SeedPropertyAsync(otherCustomer.Id);

        var (_, subB) = await SeedCompanyAndUserAsync();
        var clientB = ClientFor(subB);

        var response = await clientB.GetAsync(PropertyById(otherProperty.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Valid_Returns200AndPersists()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer = await SeedCustomerAsync(companyId);
        var seeded = await SeedPropertyAsync(customer.Id,
            streetAddress: "10 Old Rd",
            city: "OldCity",
            state: "OR",
            postalCode: "97000",
            accessNotes: null);

        var client = ClientFor(sub);
        var update = new UpdatePropertyRequest(
            StreetAddress: "20 New Rd",
            City: "NewCity",
            State: "WA",
            PostalCode: "98000",
            AccessNotes: "Ring bell twice");

        var putResponse = await client
            .PutAsJsonAsync(PropertyById(seeded.Id), update);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.NotNull(updated);
        Assert.Equal(seeded.Id, updated!.Id);
        Assert.Equal(customer.Id, updated.CustomerId);
        Assert.Equal("20 New Rd", updated.StreetAddress);
        Assert.Equal("NewCity", updated.City);
        Assert.Equal("WA", updated.State);
        Assert.Equal("98000", updated.PostalCode);
        Assert.Equal("Ring bell twice", updated.AccessNotes);
        Assert.True(updated.UpdatedAt > seeded.UpdatedAt,
            "UpdatedAt should advance after an update.");
        Assert.Equal(seeded.CreatedAt.ToUnixTimeSeconds(),
            updated.CreatedAt.ToUnixTimeSeconds());

        var getResponse = await client.GetAsync(PropertyById(seeded.Id));
        var afterGet = await getResponse.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.Equal("20 New Rd", afterGet!.StreetAddress);
        Assert.Equal("WA", afterGet.State);
        Assert.Equal("Ring bell twice", afterGet.AccessNotes);
    }

    [Fact]
    public async Task Put_ClearsAccessNotes_WhenBlank()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer = await SeedCustomerAsync(companyId);
        var seeded = await SeedPropertyAsync(customer.Id,
            accessNotes: "Original notes");

        var client = ClientFor(sub);
        var update = new UpdatePropertyRequest(
            StreetAddress: seeded.StreetAddress,
            City: seeded.City,
            State: seeded.State,
            PostalCode: seeded.PostalCode,
            AccessNotes: "   ");

        var response = await client.PutAsJsonAsync(PropertyById(seeded.Id), update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.Null(body!.AccessNotes);
    }

    [Fact]
    public async Task Put_Unknown_Returns404()
    {
        var (_, sub) = await SeedCompanyAndUserAsync();
        var client = ClientFor(sub);

        var response = await client
            .PutAsJsonAsync(PropertyById(Guid.NewGuid()), ValidUpdate());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_OtherCompanyProperty_Returns404_AndDoesNotMutate()
    {
        var (companyA, _) = await SeedCompanyAndUserAsync();
        var victimCustomer = await SeedCustomerAsync(companyA);
        var victimProperty = await SeedPropertyAsync(victimCustomer.Id,
            streetAddress: "DoNotTouch",
            city: "Safe",
            state: "OR",
            postalCode: "97000",
            accessNotes: "Sensitive");

        var (_, subB) = await SeedCompanyAndUserAsync();
        var clientB = ClientFor(subB);

        var update = new UpdatePropertyRequest(
            "Hacked", "Hackville", "XX", "00000", "pwn3d");

        var response = await clientB.PutAsJsonAsync(
            PropertyById(victimProperty.Id), update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var reread = await db.Properties.FindAsync(victimProperty.Id);
        Assert.NotNull(reread);
        Assert.Equal("DoNotTouch", reread!.StreetAddress);
        Assert.Equal("Safe", reread.City);
        Assert.Equal("OR", reread.State);
        Assert.Equal("97000", reread.PostalCode);
        Assert.Equal("Sensitive", reread.AccessNotes);
    }

    [Theory]
    [InlineData("", "Portland", "OR", "97201")]
    [InlineData("100 Elm St", "", "OR", "97201")]
    [InlineData("100 Elm St", "Portland", "", "97201")]
    [InlineData("100 Elm St", "Portland", "OR", "")]
    public async Task Put_MissingRequiredFields_Returns400(
        string street, string city, string state, string postal)
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer = await SeedCustomerAsync(companyId);
        var seeded = await SeedPropertyAsync(customer.Id);

        var client = ClientFor(sub);
        var update = new UpdatePropertyRequest(street, city, state, postal, null);

        var response = await client.PutAsJsonAsync(PropertyById(seeded.Id), update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}