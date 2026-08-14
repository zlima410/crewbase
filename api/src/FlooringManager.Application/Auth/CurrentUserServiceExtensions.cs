namespace FlooringManager.Application.Auth;

public static class CurrentUserServiceExtensions
{
    /// <summary>
    /// Resolves the caller's provisioned user, or throws. A valid token whose
    /// subject has no active user row is authenticated but not authorized, so this
    /// surfaces as 403 rather than 401 (see GlobalExceptionHandler).
    /// </summary>
    public static async Task<CurrentUser> RequireAsync(
        this ICurrentUserService currentUserService,
        CancellationToken cancellationToken = default) =>
        await currentUserService.GetAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("No provisioned user for the current token.");
}
