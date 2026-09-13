using FlooringManager.Application.Jobs;
using FlooringManager.Domain.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlooringManager.Api.Controllers.v1;

[ApiController]
[Route("api/v1/jobs")]
[Authorize]
public sealed class JobsController(IJobService jobs) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<JobListResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] JobStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default) =>
        Ok(await jobs.ListAsync(page, pageSize, status, search, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponse>> Get(Guid id, CancellationToken ct)
    {
        var job = await jobs.GetAsync(id, ct);
        return job is null ? NotFound() : Ok(job);
    }
}
