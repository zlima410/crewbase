using FlooringManager.Domain.Companies;
using FlooringManager.Domain.Customers;
using FlooringManager.Domain.Properties;
using FlooringManager.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FlooringManager.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Property> Properties => Set<Property>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var converter = new DateTimeOffsetToBinaryConverter();
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset) ||
                        property.ClrType == typeof(DateTimeOffset?))
                    {
                        property.SetValueConverter(converter);
                    }
                }
            }
        }
    }
}