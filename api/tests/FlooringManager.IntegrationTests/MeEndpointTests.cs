using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlooringManager.Domain.Companies;
using FlooringManager.Domain.Shared;
using FlooringManager.Domain.Users;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FlooringManager.IntegrationTests;

public sealed class MeEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Endpoint = "/api/v1/me";

    private sealed record MeResponse(Guid UserId, Guid CompanyId, string FirstName, string LastName, string Email, string Role);

    private async Task<(Company company, User user)> SeedUserAsync(
        Guid authProviderUserId,
        UserRole role = UserRole.Owner,
        bool isActive = true)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Pilot Flooring Co.",
            Email = "owner@pilotflooring.example",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            AuthProviderUserId = authProviderUserId,
            CompanyId = company.Id,
            FirstName = "Pilot",
            LastName = "Owner",
            Email = "owner@pilotflooring.example",
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Companies.Add(company);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (company, user);
    }

    private HttpClient CreateClient(string? token)
    {
        var client = factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        var response = await CreateClient(null).GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithMalformedToken_Returns401()
    {
        var response = await CreateClient("not-a-real-jwt").GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithWrongSignature_Returns401()
    {
        using var otherRsa = System.Security.Cryptography.RSA.Create(2048);
        var otherKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(otherRsa)
        {
            KeyId = "impostor-key"
        };

        var token = new TestJwtBuilder(factory.SigningKey)
            .WithSigningKey(otherKey)
            .Build();

        var response = await CreateClient(token).GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithWrongIssuer_Returns401()
    {
        var token = new TestJwtBuilder(factory.SigningKey)
            .WithIssuer("https://evil.example/auth/v1")
            .Build();

        var response = await CreateClient(token).GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithWrongAudience_Returns401()
    {
        var token = new TestJwtBuilder(factory.SigningKey)
            .WithAudience("anon")
            .Build();

        var response = await CreateClient(token).GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithExpiredToken_Returns401()
    {
        var token = new TestJwtBuilder(factory.SigningKey).Expired().Build();

        var response = await CreateClient(token).GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithValidToken_ReturnsUser()
    {
        var sub = Guid.NewGuid();
        var (company, user) = await SeedUserAsync(sub, role: UserRole.OfficeManager);

        var token = new TestJwtBuilder(factory.SigningKey).WithSub(sub).Build();

        var response = await CreateClient(token).GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(body);
        Assert.Equal(user.Id, body!.UserId);
        Assert.Equal(company.Id, body.CompanyId);
        Assert.Equal(user.FirstName, body.FirstName);
        Assert.Equal(user.LastName, body.LastName);
        Assert.Equal(user.Email, body.Email);
        Assert.Equal(nameof(UserRole.OfficeManager), body.Role);
    }

    [Fact]
    public async Task Get_IgnoresClientSuppliedCompanyId()
    {
        var sub = Guid.NewGuid();
        var (company, _) = await SeedUserAsync(sub);

        var forgedCompanyId = Guid.NewGuid();
        var token = new TestJwtBuilder(factory.SigningKey)
            .WithSub(sub)
            .WithClaim("company_id", forgedCompanyId.ToString())
            .Build();

        var client = CreateClient(token);
        client.DefaultRequestHeaders.Add("X-Company-Id", forgedCompanyId.ToString());

        var response = await client.GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.Equal(company.Id, body!.CompanyId);
        Assert.NotEqual(forgedCompanyId, body.CompanyId);
    }

    [Theory]
    [InlineData(UserRole.Owner)]
    [InlineData(UserRole.OfficeManager)]
    [InlineData(UserRole.CrewLead)]
    public async Task Get_ExposesRoleForAuthorization(UserRole role)
    {
        var sub = Guid.NewGuid();
        await SeedUserAsync(sub, role: role);

        var token = new TestJwtBuilder(factory.SigningKey).WithSub(sub).Build();

        var response = await CreateClient(token).GetAsync(Endpoint);
        var body = await response.Content.ReadFromJsonAsync<MeResponse>();

        Assert.Equal(role.ToString(), body!.Role);
    }

    [Fact]
    public async Task Get_WithValidToken_ButNoLocalUser_Returns403()
    {
        var token = new TestJwtBuilder(factory.SigningKey).WithSub(Guid.NewGuid()).Build();

        var response = await CreateClient(token).GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithInactiveUser_Returns403()
    {
        var sub = Guid.NewGuid();
        await SeedUserAsync(sub, isActive: false);

        var token = new TestJwtBuilder(factory.SigningKey).WithSub(sub).Build();

        var response = await CreateClient(token).GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}