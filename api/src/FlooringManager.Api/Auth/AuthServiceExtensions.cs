using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FlooringManager.Api.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddSupabaseAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SupabaseAuthOptions>()
            .Bind(configuration.GetSection(SupabaseAuthOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.JwtSecret), "Missing 'Supabase:JwtSecret'.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, ConfigureJwtBearer>();

        services.AddAuthorization();
        return services;
    }

    private sealed class ConfigureJwtBearer(IOptions<SupabaseAuthOptions> supabase)
        : IPostConfigureOptions<JwtBearerOptions>
    {
        public void PostConfigure(string? name, JwtBearerOptions options)
        {
            var s = supabase.Value;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(s.JwtSecret)),
                ValidateIssuer = !string.IsNullOrWhiteSpace(s.Issuer),
                ValidIssuer = s.Issuer,
                ValidateAudience = true,
                ValidAudience = s.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        }
    }
}