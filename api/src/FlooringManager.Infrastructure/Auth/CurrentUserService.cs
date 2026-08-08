using System.Security.Claims;
using FlooringManager.Application.Auth;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FlooringManager.Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext) : ICurrentUserService
{
    private CurrentUser? _cached;

    public async Task<CurrentUser?> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null) return _cached;

        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) return null;

        var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");

        if (!Guid.TryParse(subClaim?.Value, out var authUserId)) return null;

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.AuthProviderUserId == authUserId && u.IsActive)
            .Select(u => new CurrentUser(u.Id, u.CompanyId, u.AuthProviderUserId, u.Email, u.Role, u.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        _cached = user;
        return user;
    }
}