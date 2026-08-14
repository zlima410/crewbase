using System.Net;

namespace FlooringManager.IntegrationTests;

/// <summary>
/// Uses its own host with deliberately tiny budgets; the rest of the suite runs with
/// rate limiting off so request counts don't affect assertions.
/// </summary>
public sealed class RateLimitingTests(RateLimitApiFactory factory) : IClassFixture<RateLimitApiFactory>
{
    [Fact]
    public async Task IdentityEndpoint_BeyondItsBudget_Returns429WithRetryAfter()
    {
        var tenant = await factory.SeedTenantAsync();
        var client = factory.ClientFor(tenant.Sub);

        for (var i = 0; i < RateLimitApiFactory.IdentityPermitLimit; i++)
        {
            var allowed = await client.GetAsync("/api/v1/me");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var rejected = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Budgets_ArePerUser_SoOneTenantCannotExhaustAnother()
    {
        var noisy = await factory.SeedTenantAsync();
        var quiet = await factory.SeedTenantAsync();

        var noisyClient = factory.ClientFor(noisy.Sub);
        for (var i = 0; i <= RateLimitApiFactory.IdentityPermitLimit; i++)
        {
            await noisyClient.GetAsync("/api/v1/me");
        }

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            (await noisyClient.GetAsync("/api/v1/me")).StatusCode);

        var quietResponse = await factory.ClientFor(quiet.Sub).GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.OK, quietResponse.StatusCode);
    }

    [Fact]
    public async Task BusinessEndpoints_UseTheLargerGlobalBudget_NotTheIdentityOne()
    {
        var tenant = await factory.SeedTenantAsync();
        var client = factory.ClientFor(tenant.Sub);

        for (var i = 0; i <= RateLimitApiFactory.IdentityPermitLimit; i++)
        {
            var response = await client.GetAsync("/api/v1/customers");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
