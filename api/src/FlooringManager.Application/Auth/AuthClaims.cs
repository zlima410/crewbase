using System.Security.Claims;

namespace FlooringManager.Application.Auth;

public static class AuthClaims
{
    /// <summary>
    /// Reads the auth provider's subject id from a validated principal.
    /// </summary>
    /// <remarks>
    /// The JWT bearer handler maps <c>sub</c> to <see cref="ClaimTypes.NameIdentifier"/>
    /// when inbound claim mapping is on, so both spellings have to be checked. Anything
    /// that keys off the caller's identity should use this rather than picking one:
    /// reading only <c>sub</c> silently returns nothing, which is the kind of bug that
    /// degrades quietly instead of failing.
    /// </remarks>
    public static string? SubjectId(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true) return null;

        var claim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");

        return string.IsNullOrWhiteSpace(claim?.Value) ? null : claim.Value;
    }
}
