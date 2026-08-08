using System.Text.Json;
using FlooringManager.Api.Auth;
using FlooringManager.Api.Extensions;
using FlooringManager.Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Logging: built-in providers only for MVP, configured via appsettings.json
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddApiServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddSupabaseDatabase(builder.Configuration);
builder.Services.AddSupabaseAuth(builder.Configuration);

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .WithMethods("GET", "POST", "PUT", "OPTIONS")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
});

app.Run();

static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy";

    return context.Response.WriteAsync(JsonSerializer.Serialize(new { status }));
}

public partial class Program { }