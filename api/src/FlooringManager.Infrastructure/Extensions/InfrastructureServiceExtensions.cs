using FlooringManager.Application.Auth;
using FlooringManager.Infrastructure.Auth;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlooringManager.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        return services;
    }
    public static IServiceCollection AddSupabaseDatabase(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var conn = cfg.GetConnectionString("SupabaseDb")
                ?? throw new InvalidOperationException("Missing 'ConnectionStrings:SupabaseDb'.");
            options.UseNpgsql(conn);
        });
        return services;
    }
}