using System.Security.Cryptography;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace FlooringManager.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://test.supabase.local/auth/v1";
    public const string Audience = "authenticated";
    public const string TestKeyId = "test-key-1";

    private readonly SqliteConnection _connection;
    private readonly RSA _rsa;

    public RsaSecurityKey SigningKey { get; }

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__SupabaseDb",
            "Host=placeholder;Database=placeholder;Username=x;Password=y");
        Environment.SetEnvironmentVariable("Supabase__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Supabase__Audience", Audience);

        _rsa = RSA.Create(2048);
        SigningKey = new RsaSecurityKey(_rsa) { KeyId = TestKeyId };

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_connection));

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

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
            _rsa.Dispose();
        }
        base.Dispose(disposing);
    }
}