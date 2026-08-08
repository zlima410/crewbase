using FlooringManager.Application.Auth;
using FlooringManager.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlooringManager.Api.Controllers.v1;

[ApiController]
[Route("api/v1/me")]
[Authorize]
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
            user.Email,
            user.Role.ToString()));
    }
}

public sealed record MeResponse(
    Guid UserId,
    Guid CompanyId,
    string Email,
    string Role);