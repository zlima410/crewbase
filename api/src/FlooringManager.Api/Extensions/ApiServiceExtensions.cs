using FlooringManager.Api.HealthChecks;
using FlooringManager.Api.Middleware;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlooringManager.Api.Extensions;

public static class ApiServiceExtensions
{
    /// <summary>
    /// Registers everything the API layer owns: controllers, OpenAPI generation,
    /// standardized error handling, and health checks.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");

        return services;
    }
}