using System.Net;
using System.Net.Http.Json;
using FlooringManager.Application.Customers;
using FlooringManager.Application.Estimates;
using FlooringManager.Domain.Estimates;

namespace FlooringManager.IntegrationTests;

/// <summary>
/// Behaviour that only the real database engine can confirm: that the migrations
/// apply, that search is genuinely case-insensitive, and that number allocation holds
/// up under concurrent writers.
/// </summary>
[Trait("Category", "Postgres")]
public sealed class PostgresBehaviourTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [Fact]
    public async Task Migrations_Apply_AndTheApiIsHealthy()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CustomerSearch_IsCaseInsensitive_OnPostgres()
    {
        var tenant = await factory.SeedTenantAsync();
        var client = factory.ClientFor(tenant.Sub);

        var response = await client.GetAsync("/api/v1/customers?search=JOHNSON");
        var body = await response.Content.ReadFromJsonAsync<CustomerListResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(body!.Items, customer => customer.Id == tenant.CustomerId);
    }

    [Fact]
    public async Task EstimateNumbers_AreSequentialPerCompany()
    {
        var tenant = await factory.SeedTenantAsync();
        var client = factory.ClientFor(tenant.Sub);

        var numbers = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/estimates", Request(tenant));
            var body = await response.Content.ReadFromJsonAsync<EstimateResponse>(TestJson.Options);
            numbers.Add(body!.EstimateNumber);
        }

        Assert.Equal(["EST-0001", "EST-0002", "EST-0003"], numbers);
    }

    [Fact]
    public async Task ConcurrentEstimateCreates_ProduceDistinctGapFreeNumbers()
    {
        var tenant = await factory.SeedTenantAsync();
        const int concurrency = 12;

        var responses = await Task.WhenAll(Enumerable.Range(0, concurrency).Select(async _ =>
        {
            var response = await factory.ClientFor(tenant.Sub)
                .PostAsJsonAsync("/api/v1/estimates", Request(tenant));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            return await response.Content.ReadFromJsonAsync<EstimateResponse>(TestJson.Options);
        }));

        var numbers = responses.Select(r => r!.EstimateNumber).OrderBy(n => n).ToArray();

        Assert.Equal(concurrency, numbers.Distinct().Count());
        Assert.Equal(
            Enumerable.Range(1, concurrency).Select(i => $"EST-{i:D4}"),
            numbers);
    }

    [Fact]
    public async Task EstimateNumbering_IsIndependentPerCompany()
    {
        var first = await factory.SeedTenantAsync();
        var second = await factory.SeedTenantAsync();

        foreach (var tenant in new[] { first, second })
        {
            var response = await factory.ClientFor(tenant.Sub)
                .PostAsJsonAsync("/api/v1/estimates", Request(tenant));
            var body = await response.Content.ReadFromJsonAsync<EstimateResponse>(TestJson.Options);

            Assert.Equal("EST-0001", body!.EstimateNumber);
        }
    }

    private static CreateEstimateRequest Request(TestTenant tenant) =>
        new(
            tenant.CustomerId,
            tenant.PropertyId,
            null,
            0m,
            null,
            [
                new EstimateRoomInput(
                    null, "Living Room", 10m, 10m, 0m,
                    FlooringType.SolidHardwood, WorkType.NewInstallation, 3m, 1m)
            ]);
}
