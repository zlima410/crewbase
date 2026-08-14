using FlooringManager.Application.Auth;
using FlooringManager.Application.Common;
using FlooringManager.Application.Customers;
using FlooringManager.Application.Estimates;
using FlooringManager.Application.Properties;
using FlooringManager.Infrastructure.Auth;
using FlooringManager.Infrastructure.Common;
using FlooringManager.Infrastructure.Customers;
using FlooringManager.Infrastructure.Estimates;
using FlooringManager.Infrastructure.Persistence;
using FlooringManager.Infrastructure.Properties;
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

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICompanySequenceAllocator, CompanySequenceAllocator>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IEstimateService, EstimateService>();
        services.AddScoped<EstimateRoomSynchronizer>();

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