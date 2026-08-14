using FlooringManager.Domain.Companies;
using FlooringManager.Domain.Customers;
using FlooringManager.Domain.Estimates;
using FlooringManager.Domain.Properties;
using FlooringManager.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace FlooringManager.Infrastructure.Persistence;

/// <summary>
/// Takes a non-generic <see cref="DbContextOptions"/> so test projects can derive a
/// provider-specific context without duplicating the model configuration.
/// </summary>
public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanySequence> CompanySequences => Set<CompanySequence>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Estimate> Estimates => Set<Estimate>();
    public DbSet<EstimateRoom> EstimateRooms => Set<EstimateRoom>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
