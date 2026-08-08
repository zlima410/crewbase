using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace FlooringManager.IntegrationTests;

public sealed class TestJwtBuilder
{
    private RsaSecurityKey _signingKey;
    private string _issuer = ApiFactory.Issuer;
    private string _audience = ApiFactory.Audience;
    private Guid _sub = Guid.NewGuid();
    private DateTime _notBefore = DateTime.UtcNow.AddMinutes(-1);
    private DateTime _expires = DateTime.UtcNow.AddHours(1);
    private readonly List<Claim> _extraClaims = new();

    public TestJwtBuilder(RsaSecurityKey signingKey)
    {
        _signingKey = signingKey;
    }

    public TestJwtBuilder WithSub(Guid sub) { _sub = sub; return this; }
    public TestJwtBuilder WithSigningKey(RsaSecurityKey key) { _signingKey = key; return this; }
    public TestJwtBuilder WithIssuer(string issuer) { _issuer = issuer; return this; }
    public TestJwtBuilder WithAudience(string audience) { _audience = audience; return this; }
    public TestJwtBuilder Expired()
    {
        _expires = DateTime.UtcNow.AddHours(-1);
        _notBefore = DateTime.UtcNow.AddHours(-2);
        return this;
    }
    public TestJwtBuilder WithClaim(string type, string value)
    {
        _extraClaims.Add(new Claim(type, value));
        return this;
    }

    public string Build()
    {
        var claims = new List<Claim>
        {
            new("sub", _sub.ToString()),
            new("email", "test@example.com"),
            new("role", "authenticated"),
        };
        claims.AddRange(_extraClaims);

        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256);

        var jwt = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: _notBefore,
            expires: _expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}