using FlooringManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlooringManager.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string JwtSecret = "test-secret-thats-at-least-32-bytes-long-xxxxxx";
    public const string Issuer = "https://test.supabase.local/auth/v1";
    public const string Audience = "authenticated";

    private readonly SqliteConnection _connection;

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__SupabaseDb",
            "Host=placeholder;Database=placeholder;Username=x;Password=y");
        Environment.SetEnvironmentVariable("Supabase__JwtSecret", JwtSecret);
        Environment.SetEnvironmentVariable("Supabase__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Supabase__Audience", Audience);

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
        }
        base.Dispose(disposing);
    }
}