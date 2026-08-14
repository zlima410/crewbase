using System.Text.Json;
using FlooringManager.Api.Auth;
using FlooringManager.Api.Cors;
using FlooringManager.Api.Extensions;
using FlooringManager.Api.RateLimiting;
using FlooringManager.Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddApiServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddSupabaseDatabase(builder.Configuration);
builder.Services.AddSupabaseAuth(builder.Configuration);

builder.Services.AddWebClientCors(builder.Configuration);
builder.Services.AddApiRateLimiting(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors(CorsServiceExtensions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
}).DisableRateLimiting();

app.Run();

static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy";

    return context.Response.WriteAsync(JsonSerializer.Serialize(new { status }));
}

public partial class Program { }