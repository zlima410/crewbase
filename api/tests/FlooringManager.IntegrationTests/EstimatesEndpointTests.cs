using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlooringManager.Application.Estimates;
using FlooringManager.Domain.Companies;
using FlooringManager.Domain.Customers;
using FlooringManager.Domain.Estimates;
using FlooringManager.Domain.Properties;
using FlooringManager.Domain.Shared;
using FlooringManager.Domain.Users;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlooringManager.IntegrationTests;

public sealed class EstimatesEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Endpoint = "/api/v1/estimates";

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
        string firstName = "Sarah",
        string lastName = "Johnson")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}@example.com",
            Phone = "555-1234",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private async Task<Property> SeedPropertyAsync(
        Guid customerId,
        string streetAddress = "123 Oak Lane")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var property = new Property
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            StreetAddress = streetAddress,
            City = "Portland",
            State = "OR",
            PostalCode = "97205",
            AccessNotes = null,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        db.Properties.Add(property);
        await db.SaveChangesAsync();
        return property;
    }

    private async Task<(Guid companyId, Guid sub, Customer customer, Property property)>
        SeedFullTenantAsync()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customer = await SeedCustomerAsync(companyId);
        var property = await SeedPropertyAsync(customer.Id);
        return (companyId, sub, customer, property);
    }

    private async Task<Estimate> SeedEstimateAsync(
        Guid companyId,
        Guid customerId,
        Guid propertyId,
        EstimateStatus status = EstimateStatus.Draft,
        string number = "EST-9000",
        decimal tax = 0m)
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
            Tax = tax,
            LaborSubtotal = 0m,
            MaterialSubtotal = 0m,
            Total = tax
        };
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

    private HttpClient AnonymousClient() => factory.CreateClient();

    private static EstimateRoomInput Room(
        string name = "Living Room",
        decimal length = 20m,
        decimal width = 15m,
        decimal wastePct = 10m,
        FlooringType flooring = FlooringType.SolidHardwood,
        WorkType work = WorkType.NewInstallation,
        decimal laborRate = 4m,
        decimal materialRate = 1.25m,
        Guid? id = null) =>
        new(id, name, length, width, wastePct, flooring, work, laborRate, materialRate);

    private static CreateEstimateRequest CreateRequest(
        Guid customerId,
        Guid propertyId,
        decimal tax = 0m,
        string? notes = null,
        params EstimateRoomInput[] rooms) =>
        new(customerId, propertyId, null, tax, notes, rooms.ToList());

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        var response = await AnonymousClient().PostAsJsonAsync(
            Endpoint,
            new CreateEstimateRequest(Guid.NewGuid(), Guid.NewGuid(), null, 0m, null,
                new List<EstimateRoomInput>()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnprovisionedUser_Returns403()
    {
        var client = ClientFor(Guid.NewGuid());

        var response = await client.PostAsJsonAsync(
            Endpoint,
            new CreateEstimateRequest(Guid.NewGuid(), Guid.NewGuid(), null, 0m, null,
                new List<EstimateRoomInput>()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownCustomer_Returns404()
    {
        var (_, sub, _, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var response = await client.PostAsJsonAsync(
            Endpoint,
            CreateRequest(customerId: Guid.NewGuid(), propertyId: property.Id, rooms: Room()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownProperty_Returns404()
    {
        var (_, sub, customer, _) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var response = await client.PostAsJsonAsync(
            Endpoint,
            CreateRequest(customerId: customer.Id, propertyId: Guid.NewGuid(), rooms: Room()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CustomerFromAnotherCompany_Returns404()
    {
        var (_, _, otherCustomer, _) = await SeedFullTenantAsync();
        var (_, subB, _, myProperty) = await SeedFullTenantAsync();
        var clientB = ClientFor(subB);

        var response = await clientB.PostAsJsonAsync(
            Endpoint,
            CreateRequest(customerId: otherCustomer.Id, propertyId: myProperty.Id, rooms: Room()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_PropertyBelongsToDifferentCustomer_Returns404()
    {
        var (companyId, sub) = await SeedCompanyAndUserAsync();
        var customerA = await SeedCustomerAsync(companyId, "Ada", "Alpha");
        var customerB = await SeedCustomerAsync(companyId, "Bea", "Beta");
        var propertyOfB = await SeedPropertyAsync(customerB.Id, "9 Rental Rd");
        var client = ClientFor(sub);

        var response = await client.PostAsJsonAsync(
            Endpoint,
            CreateRequest(customerId: customerA.Id, propertyId: propertyOfB.Id, rooms: Room()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_PropertyFromAnotherCompany_Returns404()
    {
        var (_, _, otherCustomer, otherProperty) = await SeedFullTenantAsync();

        var (_, subB, _, _) = await SeedFullTenantAsync();
        var clientB = ClientFor(subB);

        var response = await clientB.PostAsJsonAsync(
            Endpoint,
            CreateRequest(customerId: otherCustomer.Id, propertyId: otherProperty.Id, rooms: Room()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_SingleRoom_Returns201_WithLocationAndComputedTotals()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        // Example: 20 × 15 @ 10% → billable 330; labor $4, material $1.25.
        // Expected: labor 1320, material 412.5, subtotal 1732.5, total 1732.5.
        var request = CreateRequest(customer.Id, property.Id,
            rooms: Room("Living Room", 20m, 15m, 10m,
                laborRate: 4m, materialRate: 1.25m));

        var response = await client.PostAsJsonAsync(Endpoint, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Assert.NotNull(response.Headers.Location);
        var body = await response.Content.ReadFromJsonAsync<EstimateResponse>();
        Assert.NotNull(body);
        Assert.Contains($"/api/v1/estimates/{body!.Id}",
            response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

        Assert.Equal(EstimateStatus.Draft, body.Status);
        Assert.False(string.IsNullOrWhiteSpace(body.EstimateNumber));
        Assert.StartsWith("EST-", body.EstimateNumber);

        Assert.Equal(customer.Id, body.CustomerId);
        Assert.Equal(property.Id, body.PropertyId);
        Assert.Equal($"{customer.FirstName} {customer.LastName}", body.CustomerName);
        Assert.Contains(property.StreetAddress, body.PropertyAddress);

        Assert.Equal(1320m, body.LaborSubtotal);
        Assert.Equal(412.5m, body.MaterialSubtotal);
        Assert.Equal(1732.5m, body.Subtotal);
        Assert.Equal(0m, body.Tax);
        Assert.Equal(1732.5m, body.Total);

        var room = Assert.Single(body.Rooms);
        Assert.Equal("Living Room", room.Name);
        Assert.Equal(300m, room.SquareFeet);
        Assert.Equal(330m, room.BillableSquareFeet);
        Assert.Equal(1320m, room.LaborCost);
        Assert.Equal(412.5m, room.MaterialCost);
        Assert.Equal(1732.5m, room.RoomTotal);
        Assert.Equal(0, room.Position);
    }

    [Fact]
    public async Task Post_MultiRoom_ComputesAggregates()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        // Living: 20×15 @ 10%, labor 4, material 1.25  → labor 1320, material 412.5
        // Hallway: 15.5×4 @ 5%, labor 5, material 1.5  →
        //   ceil(15.5)=16, 16×4=64, 64×1.05=67.2       → labor 336, material 100.8
        // Labor subtotal:    1656
        // Material subtotal: 513.3
        // Subtotal:          2169.3
        // Tax:               50
        // Total:             2219.3
        var request = CreateRequest(customer.Id, property.Id, tax: 50m,
            rooms: new[]
            {
                Room("Living Room", 20m, 15m, 10m, laborRate: 4m, materialRate: 1.25m),
                Room("Hallway",     15.5m, 4m, 5m, laborRate: 5m, materialRate: 1.5m),
            });

        var response = await client.PostAsJsonAsync(Endpoint, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<EstimateResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.Rooms.Count);

        Assert.Equal(1656m, body.LaborSubtotal);
        Assert.Equal(513.3m, body.MaterialSubtotal);
        Assert.Equal(2169.3m, body.Subtotal);
        Assert.Equal(50m, body.Tax);
        Assert.Equal(2219.3m, body.Total);

        Assert.Equal("Living Room", body.Rooms[0].Name);
        Assert.Equal(0, body.Rooms[0].Position);
        Assert.Equal("Hallway", body.Rooms[1].Name);
        Assert.Equal(1, body.Rooms[1].Position);
    }

    [Fact]
    public async Task Post_EmptyRooms_Returns201_WithZeroTotals()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var request = CreateRequest(customer.Id, property.Id, tax: 0m);

        var response = await client.PostAsJsonAsync(Endpoint, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<EstimateResponse>();
        Assert.NotNull(body);
        Assert.Empty(body!.Rooms);
        Assert.Equal(0m, body.LaborSubtotal);
        Assert.Equal(0m, body.MaterialSubtotal);
        Assert.Equal(0m, body.Total);
        Assert.Equal(EstimateStatus.Draft, body.Status);
    }

    [Fact]
    public async Task Post_TrimsNotes_AndBlankNotesBecomesNull()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var withText = await client.PostAsJsonAsync(
            Endpoint,
            CreateRequest(customer.Id, property.Id, notes: "  needs sanding  ", rooms: Room()));
        var textBody = await withText.Content.ReadFromJsonAsync<EstimateResponse>();
        Assert.Equal("needs sanding", textBody!.Notes);

        var withBlank = await client.PostAsJsonAsync(
            Endpoint,
            CreateRequest(customer.Id, property.Id, notes: "   ", rooms: Room()));
        var blankBody = await withBlank.Content.ReadFromJsonAsync<EstimateResponse>();
        Assert.Null(blankBody!.Notes);
    }

    public static TheoryData<EstimateRoomInput> InvalidRoomInputs => new()
    {
        // Missing name
        new EstimateRoomInput(null, "", 20m, 15m, 10m,
            FlooringType.SolidHardwood, WorkType.NewInstallation, 4m, 1.25m),

        // Length below Range(0.0001, 1000)
        new EstimateRoomInput(null, "Bad", 0m, 15m, 10m,
            FlooringType.SolidHardwood, WorkType.NewInstallation, 4m, 1.25m),

        // Width above Range(0.0001, 1000)
        new EstimateRoomInput(null, "Bad", 20m, 1001m, 10m,
            FlooringType.SolidHardwood, WorkType.NewInstallation, 4m, 1.25m),

        // Waste above Range(0, 100)
        new EstimateRoomInput(null, "Bad", 20m, 15m, 101m,
            FlooringType.SolidHardwood, WorkType.NewInstallation, 4m, 1.25m),

        // Labor rate above Range(0, 10_000)
        new EstimateRoomInput(null, "Bad", 20m, 15m, 10m,
            FlooringType.SolidHardwood, WorkType.NewInstallation, 10_001m, 1.25m),

        // Material rate below Range(0, 10_000)
        new EstimateRoomInput(null, "Bad", 20m, 15m, 10m,
            FlooringType.SolidHardwood, WorkType.NewInstallation, 4m, -1m),
    };

    [Theory]
    [MemberData(nameof(InvalidRoomInputs))]
    public async Task Post_InvalidRoomInput_Returns400(EstimateRoomInput badRoom)
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var response = await client.PostAsJsonAsync(
            Endpoint,
            new CreateEstimateRequest(customer.Id, property.Id, null, 0m, null,
                new List<EstimateRoomInput> { badRoom }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_NegativeTax_Returns400()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var response = await client.PostAsJsonAsync(
            Endpoint,
            new CreateEstimateRequest(customer.Id, property.Id, null, -1m, null,
                new List<EstimateRoomInput> { Room() }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ExistingEstimate_Returns200_WithRoomsOrderedByPosition()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var created = await client.PostAsJsonAsync(
            Endpoint,
            CreateRequest(customer.Id, property.Id,
                rooms: new[]
                {
                    Room("Room A"),
                    Room("Room B"),
                    Room("Room C"),
                }));
        var createdBody = await created.Content.ReadFromJsonAsync<EstimateResponse>();
        Assert.NotNull(createdBody);

        var getResponse = await client.GetAsync($"{Endpoint}/{createdBody!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var body = await getResponse.Content.ReadFromJsonAsync<EstimateResponse>();
        Assert.NotNull(body);
        Assert.Equal(3, body!.Rooms.Count);
        Assert.Equal(new[] { "Room A", "Room B", "Room C" },
            body.Rooms.Select(r => r.Name));
        Assert.Equal(new[] { 0, 1, 2 }, body.Rooms.Select(r => r.Position));
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
    public async Task Get_EstimateFromAnotherCompany_Returns404()
    {
        var (companyA, _, customerA, propertyA) = await SeedFullTenantAsync();
        var other = await SeedEstimateAsync(companyA, customerA.Id, propertyA.Id);

        var (_, subB) = await SeedCompanyAndUserAsync();
        var clientB = ClientFor(subB);

        var response = await clientB.GetAsync($"{Endpoint}/{other.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_ReplacesRoomList_AddsUpdatesAndDeletes()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var initial = await client.PostAsJsonAsync(
            Endpoint,
            CreateRequest(customer.Id, property.Id,
                rooms: new[]
                {
                    Room("Keep",    10m, 10m, 0m,  laborRate: 3m, materialRate: 1m),
                    Room("Modify",  10m, 10m, 0m,  laborRate: 3m, materialRate: 1m),
                    Room("Delete",  10m, 10m, 0m,  laborRate: 3m, materialRate: 1m),
                }));
        var initialBody = await initial.Content.ReadFromJsonAsync<EstimateResponse>();
        Assert.NotNull(initialBody);

        var keep = initialBody!.Rooms.First(r => r.Name == "Keep");
        var modify = initialBody.Rooms.First(r => r.Name == "Modify");

        var update = new UpdateEstimateRequest(
            customer.Id, property.Id, null, 0m, null,
            new List<EstimateRoomInput>
            {
                Room("Keep",    10m, 10m, 0m, laborRate: 3m, materialRate: 1m, id: keep.Id),
                Room("Modified", 20m, 15m, 10m, laborRate: 4m, materialRate: 1.25m, id: modify.Id),
                Room("Added",   5m, 5m, 0m, laborRate: 2m, materialRate: 0.5m),
            });

        var putResponse = await client.PutAsJsonAsync($"{Endpoint}/{initialBody.Id}", update);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var body = await putResponse.Content.ReadFromJsonAsync<EstimateResponse>();
        Assert.NotNull(body);
        Assert.Equal(3, body!.Rooms.Count);
        Assert.DoesNotContain(body.Rooms, r => r.Name == "Delete");

        Assert.Contains(body.Rooms, r => r.Id == keep.Id && r.Name == "Keep");

        var modifiedNow = body.Rooms.Single(r => r.Id == modify.Id);
        Assert.Equal("Modified", modifiedNow.Name);
        Assert.Equal(330m, modifiedNow.BillableSquareFeet);
        Assert.Equal(1320m, modifiedNow.LaborCost);
        Assert.Equal(412.5m, modifiedNow.MaterialCost);

        var previousIds = initialBody.Rooms.Select(r => r.Id).ToHashSet();
        var added = body.Rooms.Single(r => r.Name == "Added");
        Assert.DoesNotContain(added.Id, previousIds);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deletedId = initialBody.Rooms.Single(r => r.Name == "Delete").Id;
        Assert.False(await db.EstimateRooms.AnyAsync(r => r.Id == deletedId));
    }

    [Fact]
    public async Task Put_RecalculatesTotalsFromNewRoomsAndTax()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var initial = await client.PostAsJsonAsync(
            Endpoint,
            CreateRequest(customer.Id, property.Id,
                rooms: Room("Small", 5m, 5m, 0m, laborRate: 2m, materialRate: 1m)));
        var initialBody = await initial.Content.ReadFromJsonAsync<EstimateResponse>();
        Assert.Equal(75m, initialBody!.Total); // 25 * (2+1)

        var update = new UpdateEstimateRequest(
            customer.Id, property.Id, null, 100m, null,
            new List<EstimateRoomInput>
            {
                Room("Living Room", 20m, 15m, 10m, laborRate: 4m, materialRate: 1.25m),
            });

        var putResponse = await client.PutAsJsonAsync($"{Endpoint}/{initialBody.Id}", update);
        var body = await putResponse.Content.ReadFromJsonAsync<EstimateResponse>();

        Assert.Equal(1320m, body!.LaborSubtotal);
        Assert.Equal(412.5m, body.MaterialSubtotal);
        Assert.Equal(1732.5m, body.Subtotal);
        Assert.Equal(100m, body.Tax);
        Assert.Equal(1832.5m, body.Total);
    }

    [Theory]
    [InlineData(EstimateStatus.Sent)]
    [InlineData(EstimateStatus.Accepted)]
    [InlineData(EstimateStatus.Rejected)]
    [InlineData(EstimateStatus.Expired)]
    public async Task Put_NonDraftStatus_Returns404_AndDoesNotMutate(EstimateStatus status)
    {
        var (companyId, sub, customer, property) = await SeedFullTenantAsync();
        var locked = await SeedEstimateAsync(companyId, customer.Id, property.Id,
            status: status, number: $"EST-LOCKED-{status}");
        var client = ClientFor(sub);

        var update = new UpdateEstimateRequest(
            customer.Id, property.Id, null, 999m, "should not persist",
            new List<EstimateRoomInput> { Room() });

        var response = await client.PutAsJsonAsync($"{Endpoint}/{locked.Id}", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var reread = await db.Estimates.Include(e => e.Rooms).FirstAsync(e => e.Id == locked.Id);
        Assert.Equal(status, reread.Status);
        Assert.Empty(reread.Rooms);
        Assert.Null(reread.Notes);
        Assert.Equal(0m, reread.Tax);
    }

    [Fact]
    public async Task Put_EstimateFromAnotherCompany_Returns404()
    {
        var (companyA, _, customerA, propertyA) = await SeedFullTenantAsync();
        var victim = await SeedEstimateAsync(companyA, customerA.Id, propertyA.Id);

        var (_, subB, otherCustomer, otherProperty) = await SeedFullTenantAsync();
        var clientB = ClientFor(subB);

        var update = new UpdateEstimateRequest(
            otherCustomer.Id, otherProperty.Id, null, 0m, "pwn",
            new List<EstimateRoomInput> { Room() });

        var response = await clientB.PutAsJsonAsync($"{Endpoint}/{victim.Id}", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var reread = await db.Estimates.FirstAsync(e => e.Id == victim.Id);
        Assert.Null(reread.Notes);
        Assert.Equal(customerA.Id, reread.CustomerId);
    }

    [Fact]
    public async Task Put_UnknownCustomer_Returns404()
    {
        var (_, sub, customer, property) = await SeedFullTenantAsync();
        var client = ClientFor(sub);

        var created = await client.PostAsJsonAsync(
            Endpoint,
            CreateRequest(customer.Id, property.Id, rooms: Room()));
        var body = await created.Content.ReadFromJsonAsync<EstimateResponse>();

        var badUpdate = new UpdateEstimateRequest(
            Guid.NewGuid(), property.Id, null, 0m, null,
            new List<EstimateRoomInput> { Room() });

        var response = await client.PutAsJsonAsync($"{Endpoint}/{body!.Id}", badUpdate);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_ScopedToCompany()
    {
        var (companyA, subA, customerA, propertyA) = await SeedFullTenantAsync();
        for (var i = 0; i < 3; i++)
            await SeedEstimateAsync(companyA, customerA.Id, propertyA.Id,
                number: $"EST-A-{i:D4}");

        var (companyB, subB, customerB, propertyB) = await SeedFullTenantAsync();
        for (var i = 0; i < 2; i++)
            await SeedEstimateAsync(companyB, customerB.Id, propertyB.Id,
                number: $"EST-B-{i:D4}");

        var listA = await ClientFor(subA).GetFromJsonAsync<EstimateListResponse>(Endpoint);
        var listB = await ClientFor(subB).GetFromJsonAsync<EstimateListResponse>(Endpoint);

        Assert.Equal(3, listA!.Total);
        Assert.All(listA.Items, i => Assert.StartsWith("EST-A-", i.EstimateNumber));

        Assert.Equal(2, listB!.Total);
        Assert.All(listB.Items, i => Assert.StartsWith("EST-B-", i.EstimateNumber));
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        var (companyId, sub, customer, property) = await SeedFullTenantAsync();
        await SeedEstimateAsync(companyId, customer.Id, property.Id,
            status: EstimateStatus.Draft, number: "EST-D-1");
        await SeedEstimateAsync(companyId, customer.Id, property.Id,
            status: EstimateStatus.Draft, number: "EST-D-2");
        await SeedEstimateAsync(companyId, customer.Id, property.Id,
            status: EstimateStatus.Sent, number: "EST-S-1");
        await SeedEstimateAsync(companyId, customer.Id, property.Id,
            status: EstimateStatus.Accepted, number: "EST-A-1");

        var client = ClientFor(sub);

        var drafts = await client.GetFromJsonAsync<EstimateListResponse>(
            $"{Endpoint}?status={EstimateStatus.Draft}");
        Assert.Equal(2, drafts!.Total);
        Assert.All(drafts.Items, i => Assert.Equal(EstimateStatus.Draft, i.Status));

        var sent = await client.GetFromJsonAsync<EstimateListResponse>(
            $"{Endpoint}?status={EstimateStatus.Sent}");
        Assert.Equal(1, sent!.Total);
        Assert.Equal("EST-S-1", sent.Items[0].EstimateNumber);
    }

    [Fact]
    public async Task List_Paginates_AndOrdersByCreatedDateDesc()
    {
        var (companyId, sub, customer, property) = await SeedFullTenantAsync();

        var seededNumbers = new List<string>();
        for (var i = 0; i < 15; i++)
        {
            var number = $"EST-P-{i:D4}";
            await SeedEstimateAsync(companyId, customer.Id, property.Id,
                number: number);
            seededNumbers.Add(number);
            await Task.Delay(5);
        }

        var client = ClientFor(sub);
        var page1 = await client.GetFromJsonAsync<EstimateListResponse>(
            $"{Endpoint}?page=1&pageSize=10");
        var page2 = await client.GetFromJsonAsync<EstimateListResponse>(
            $"{Endpoint}?page=2&pageSize=10");

        Assert.NotNull(page1);
        Assert.NotNull(page2);

        Assert.Equal(15, page1!.Total);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(5, page2!.Items.Count);

        Assert.Equal(seededNumbers[^1], page1.Items.First().EstimateNumber);

        var ids1 = page1.Items.Select(i => i.Id).ToHashSet();
        var ids2 = page2.Items.Select(i => i.Id).ToHashSet();
        Assert.Empty(ids1.Intersect(ids2));
    }

    [Fact]
    public async Task EstimateNumber_IsSequential_PerCompany()
    {
        var (_, subA, customerA, propertyA) = await SeedFullTenantAsync();
        var (_, subB, customerB, propertyB) = await SeedFullTenantAsync();

        var clientA = ClientFor(subA);
        var clientB = ClientFor(subB);

        var a1 = await Post(clientA, customerA.Id, propertyA.Id);
        var a2 = await Post(clientA, customerA.Id, propertyA.Id);
        var b1 = await Post(clientB, customerB.Id, propertyB.Id);
        var a3 = await Post(clientA, customerA.Id, propertyA.Id);

        Assert.Equal("EST-0001", a1.EstimateNumber);
        Assert.Equal("EST-0002", a2.EstimateNumber);
        Assert.Equal("EST-0003", a3.EstimateNumber);
        Assert.Equal("EST-0001", b1.EstimateNumber);

        static async Task<EstimateResponse> Post(HttpClient c, Guid cust, Guid prop)
        {
            var res = await c.PostAsJsonAsync("/api/v1/estimates",
                new CreateEstimateRequest(cust, prop, null, 0m, null,
                    new List<EstimateRoomInput> { Room() }));
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            return (await res.Content.ReadFromJsonAsync<EstimateResponse>())!;
        }
    }
}