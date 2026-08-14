namespace FlooringManager.Api.Cors;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Exact origins allowed to call the API, e.g. "https://app.example.com".
    /// Wildcards are rejected: the browser SPA sends a bearer token, and a
    /// permissive origin list is the one thing standing between a hostile page and
    /// a scripted request on a signed-in user's behalf.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];
}

public static class CorsServiceExtensions
{
    public const string PolicyName = "web-clients";

    public static IServiceCollection AddWebClientCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

        var origins = options.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (origins.Contains("*"))
            throw new InvalidOperationException(
                "'Cors:AllowedOrigins' must list exact origins; '*' is not supported.");

        services.AddCors(cors => cors.AddPolicy(PolicyName, policy =>
        {
            if (origins.Length == 0)
            {
                policy.WithOrigins().AllowAnyHeader().AllowAnyMethod();
                return;
            }

            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS");
        }));

        return services;
    }
}
