using FlooringManager.Application.Estimates;
using FlooringManager.Domain.Estimates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlooringManager.Api.Controllers.v1;

[ApiController]
[Route("api/v1/estimates")]
[Authorize]
public sealed class EstimatesController(IEstimateService estimates) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EstimateListResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] EstimateStatus? status = null,
        CancellationToken ct = default) =>
        Ok(await estimates.ListAsync(page, pageSize, status, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EstimateResponse>> Get(Guid id, CancellationToken ct)
    {
        var e = await estimates.GetAsync(id, ct);
        return e is null ? NotFound() : Ok(e);
    }

    [HttpPost]
    public async Task<ActionResult<EstimateResponse>> Create(
        [FromBody] CreateEstimateRequest request, CancellationToken ct)
    {
        var e = await estimates.CreateAsync(request, ct);
        return e is null ? NotFound() : CreatedAtAction(nameof(Get), new { id = e.Id }, e);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EstimateResponse>> Update(
        Guid id, [FromBody] UpdateEstimateRequest request, CancellationToken ct)
    {
        var e = await estimates.UpdateAsync(id, request, ct);
        return e is null ? NotFound() : Ok(e);
    }
}