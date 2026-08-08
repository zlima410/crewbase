using FlooringManager.Application.Auth;
using FlooringManager.Infrastructure.Auth;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlooringManager.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SupabaseDb") ?? throw new InvalidOperationException("Missing required connection string 'SupabaseDb'. " + "Set it with: dotnet user-secrets set \"ConnectionStrings:SupabaseDb\" \"...\"");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}