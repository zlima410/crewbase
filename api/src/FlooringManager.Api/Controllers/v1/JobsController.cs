using FlooringManager.Application.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlooringManager.Api.Controllers.v1;

[ApiController]
[Route("api/v1/jobs")]
[Authorize]
public sealed class JobsController(IJobService jobs) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponse>> Get(Guid id, CancellationToken ct)
    {
        var job = await jobs.GetAsync(id, ct);
        return job is null ? NotFound() : Ok(job);
    }
}
