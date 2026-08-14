using FlooringManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FlooringManager.IntegrationTests;

/// <summary>
/// Runs the API against a throwaway Postgres container, i.e. the same engine
/// production uses.
/// </summary>
/// <remarks>
/// SQLite is fast but it is not Postgres: it cannot exercise ILIKE, real row-level
/// locking, offset-aware timestamps, or whether the EF migrations actually apply.
/// This factory calls <c>Migrate()</c> rather than <c>EnsureCreated()</c> so a
/// missing or broken migration fails a test instead of production.
///
/// Requires a Docker daemon, so the tests using it are tagged
/// <c>Category=Postgres</c> and run as their own CI job.
/// </remarks>
public sealed class PostgresApiFactory : ApiFactoryBase, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:16-alpine").Build();

    protected override void ConfigureDatabase(IServiceCollection services) =>
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(_container.GetConnectionString()));

    protected override void InitializeDatabase(ApplicationDbContext db) => db.Database.Migrate();

    Task IAsyncLifetime.InitializeAsync() => _container.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _container.DisposeAsync();
    }
}
