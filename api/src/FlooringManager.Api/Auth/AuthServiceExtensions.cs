using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FlooringManager.Api.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddSupabaseAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(SupabaseAuthOptions.SectionName)
            .Get<SupabaseAuthOptions>() ?? throw new InvalidOperationException("Missing 'Supabase' configuration section.");

        if (string.IsNullOrWhiteSpace(options.JwtSecret))
            throw new InvalidOperationException("Missing 'Supabase:JwtSecret'.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.JwtSecret)),
                    ValidateIssuer = !string.IsNullOrWhiteSpace(options.Issuer),
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
        return services;
    }
}