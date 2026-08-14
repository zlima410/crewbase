using FlooringManager.Api.RateLimiting;
using FlooringManager.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FlooringManager.Api.Controllers.v1;

[ApiController]
[Route("api/v1/me")]
[Authorize]
[EnableRateLimiting(RateLimitingServiceExtensions.IdentityPolicy)]
public sealed class MeController(ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
    {
        var user = await currentUser.GetAsync(cancellationToken);

        if (user is null) return Forbid();

        return Ok(new MeResponse(
            user.UserId,
            user.CompanyId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role.ToString()));
    }
}

public sealed record MeResponse(
    Guid UserId,
    Guid CompanyId,
    string FirstName,
    string LastName,
    string Email,
    string Role);