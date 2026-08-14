using System.Globalization;
using System.Threading.RateLimiting;
using FlooringManager.Application.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FlooringManager.Api.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Set false in test hosts so limits never make assertions flaky.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Requests allowed per partition per <see cref="WindowSeconds"/>.</summary>
    public int PermitLimit { get; init; } = 300;

    public int WindowSeconds { get; init; } = 60;

    /// <summary>Tighter budget for identity endpoints, which are the cheapest to abuse.</summary>
    public int IdentityPermitLimit { get; init; } = 30;
}

public static class RateLimitingServiceExtensions
{
    /// <summary>Applied to /api/v1/me and any future identity or auth endpoint.</summary>
    public const string IdentityPolicy = "identity";

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc6585#section-4",
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                        Detail = "You have made too many requests. Please wait and try again.",
                        Instance = context.HttpContext.Request.Path
                    },
                    cancellationToken);
            };

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                Partition(context, options, options.PermitLimit));

            limiter.AddPolicy(IdentityPolicy, context =>
                Partition(context, options, options.IdentityPermitLimit));
        });

        return services;
    }

    private static RateLimitPartition<string> Partition(
        HttpContext context,
        RateLimitingOptions options,
        int permitLimit) =>
        options.Enabled
            ? RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(options.WindowSeconds)
                })
            : RateLimitPartition.GetNoLimiter(PartitionKey(context));

    /// <summary>
    /// Partition by authenticated subject when available so one tenant's traffic
    /// cannot exhaust another's budget, and fall back to remote IP for anonymous
    /// callers. The "user:"/"ip:" prefixes keep the two key spaces from colliding.
    /// </summary>
    private static string PartitionKey(HttpContext context)
    {
        var subject = AuthClaims.SubjectId(context.User);

        return subject is not null
            ? $"user:{subject}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
