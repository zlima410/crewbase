using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FlooringManager.Api.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddSupabaseAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(SupabaseAuthOptions.SectionName).Get<SupabaseAuthOptions>() ?? throw new InvalidOperationException("Missing 'Supabase' configuration section.");

        if (string.IsNullOrWhiteSpace(options.Issuer))
            throw new InvalidOperationException("Missing 'Supabase:Issuer'.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MetadataAddress = $"{options.Issuer.TrimEnd('/')}/.well-known/openid-configuration";
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();
        return services;
    }
}