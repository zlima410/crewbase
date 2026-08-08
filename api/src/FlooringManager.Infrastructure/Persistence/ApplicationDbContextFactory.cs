using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FlooringManager.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string ApiUserSecretsId = "96deec59-939b-46a2-8282-c5d2aabb78fa";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(ApiUserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("SupabaseDb") ?? throw new InvalidOperationException("Missing 'ConnectionStrings:SupabaseDb'. Set it with: " + "dotnet user-secrets set \"ConnectionStrings:SupabaseDb\" \"...\" " + "--project src/FlooringManager.Api");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}