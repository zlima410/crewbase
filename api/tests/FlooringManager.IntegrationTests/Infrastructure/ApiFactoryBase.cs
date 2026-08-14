using System.Security.Cryptography;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FlooringManager.IntegrationTests;

/// <summary>
/// Shared test host: a locally-signed JWT setup standing in for Supabase, plus a
/// database provider chosen by the derived factory.
/// </summary>
/// <remarks>
/// Configuration is applied with <c>UseSetting</c> rather than process environment
/// variables so test classes running in parallel each get their own settings instead
/// of racing over shared process state.
/// </remarks>
public abstract class ApiFactoryBase : WebApplicationFactory<Program>
{
    public const string Issuer = "https://test.supabase.local/auth/v1";
    public const string Audience = "authenticated";
    public const string TestKeyId = "test-key-1";

    private readonly RSA _rsa;

    public RsaSecurityKey SigningKey { get; }

    protected ApiFactoryBase()
    {
        _rsa = RSA.Create(2048);
        SigningKey = new RsaSecurityKey(_rsa) { KeyId = TestKeyId };
    }

    /// <summary>Registers the DbContext for this factory's provider.</summary>
    protected abstract void ConfigureDatabase(IServiceCollection services);

    /// <summary>Creates the schema once the host exists.</summary>
    protected abstract void InitializeDatabase(ApplicationDbContext db);

    /// <summary>Hook for provider- or scenario-specific settings.</summary>
    protected virtual void ConfigureSettings(IWebHostBuilder builder) { }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:SupabaseDb",
            "Host=placeholder;Database=placeholder;Username=x;Password=y");
        builder.UseSetting("Supabase:Issuer", Issuer);
        builder.UseSetting("Supabase:Audience", Audience);

        builder.UseSetting("RateLimiting:Enabled", "false");

        ConfigureSettings(builder);

        builder.ConfigureLogging(logging =>
        {
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            ConfigureDatabase(services);

            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                o =>
                {
                    o.MetadataAddress = null!;
                    o.ConfigurationManager = null!;
                    o.Configuration = null;
                    o.TokenValidationParameters.IssuerSigningKey = SigningKey;
                    o.TokenValidationParameters.IssuerSigningKeys = null;
                    o.TokenValidationParameters.IssuerSigningKeyResolver = null;
                    o.TokenValidationParameters.ValidateIssuerSigningKey = true;
                });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        InitializeDatabase(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _rsa.Dispose();
        base.Dispose(disposing);
    }
}
