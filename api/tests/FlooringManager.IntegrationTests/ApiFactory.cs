using FlooringManager.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlooringManager.IntegrationTests;

/// <summary>
/// Fast in-memory SQLite host for the bulk of the endpoint suite.
/// </summary>
/// <remarks>
/// Each test class gets its own factory, and therefore its own connection and its own
/// isolated database. Behaviour that genuinely depends on Postgres (case-insensitive
/// search, row locking behind number allocation, migrations actually applying) is
/// covered by <see cref="PostgresApiFactory"/> instead.
/// </remarks>
public class ApiFactory : ApiFactoryBase
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ApiFactory() => _connection.Open();

    protected override void ConfigureDatabase(IServiceCollection services)
    {
        services.AddDbContext<SqliteApplicationDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<ApplicationDbContext>(sp => sp.GetRequiredService<SqliteApplicationDbContext>());
    }

    protected override void InitializeDatabase(ApplicationDbContext db) => db.Database.EnsureCreated();

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }
}
