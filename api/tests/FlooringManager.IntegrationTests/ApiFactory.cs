using FlooringManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlooringManager.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSecret = "test-secret-thats-at-least-32-bytes-long-xxxxxx";
    public const string Issuer = "https://test.supabase.local/auth/v1";
    public const string Audience = "authenticated";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public Task InitializeAsync()
    {
        _connection.Open();
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SupabaseDb"] = "DataSource=:memory:",
                ["Supabase:JwtSecret"] = JwtSecret,
                ["Supabase:Issuer"] = Issuer,
                ["Supabase:Audience"] = Audience
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }
}