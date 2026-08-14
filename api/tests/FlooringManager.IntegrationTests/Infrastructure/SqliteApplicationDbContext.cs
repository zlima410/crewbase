using FlooringManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FlooringManager.IntegrationTests;

/// <summary>
/// SQLite-only variant of the production context.
/// </summary>
/// <remarks>
/// SQLite has no native offset-aware timestamp type, so DateTimeOffset must be
/// converted for ordering and comparison to work. That is a test-host concern, and
/// keeping it here rather than in <see cref="ApplicationDbContext"/> means the
/// production model stays free of provider workarounds the real database never needs.
/// </remarks>
public sealed class SqliteApplicationDbContext(DbContextOptions options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var converter = new DateTimeOffsetToBinaryConverter();

        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => property.ClrType == typeof(DateTimeOffset)
                || property.ClrType == typeof(DateTimeOffset?)))
        {
            property.SetValueConverter(converter);
        }
    }
}
