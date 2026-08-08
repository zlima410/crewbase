using FlooringManager.Domain.Shared;

namespace FlooringManager.Application.Auth;

public sealed record CurrentUser(Guid UserId, Guid CompanyId, Guid AuthProviderUserId, string Email, UserRole Role, bool IsActive);

public interface ICurrentUserService
{
    Task<CurrentUser?> GetAsync(CancellationToken cancellationToken = default);
}