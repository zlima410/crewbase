using Microsoft.AspNetCore.Hosting;

namespace FlooringManager.IntegrationTests;

/// <summary>
/// SQLite host with rate limiting enabled and budgets small enough to trip within a
/// single test.
/// </summary>
public sealed class RateLimitApiFactory : ApiFactory
{
    public const int PermitLimit = 20;
    public const int IdentityPermitLimit = 2;

    protected override void ConfigureSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:Enabled", "true");
        builder.UseSetting("RateLimiting:PermitLimit", PermitLimit.ToString());
        builder.UseSetting("RateLimiting:WindowSeconds", "60");
        builder.UseSetting("RateLimiting:IdentityPermitLimit", IdentityPermitLimit.ToString());
    }
}
