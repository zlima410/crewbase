using FlooringManager.Application.Estimates;
using FlooringManager.Application.Jobs;
using FlooringManager.Domain.Estimates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlooringManager.Api.Controllers.v1;

[ApiController]
[Route("api/v1/estimates")]
[Authorize]
public sealed class EstimatesController(
    IEstimateService estimates,
    IEstimateAcceptanceService acceptance) : ControllerBase
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

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<EstimateResponse>> Send(Guid id, CancellationToken ct)
    {
        var e = await estimates.SendAsync(id, ct);
        return e is null ? NotFound() : Ok(e);
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<JobResponse>> Accept(Guid id, CancellationToken ct)
    {
        var result = await acceptance.AcceptAsync(id, ct);
        return result.Outcome switch
        {
            AcceptEstimateOutcome.NotFound => NotFound(),
            AcceptEstimateOutcome.InvalidStatus => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Estimate cannot be accepted.",
                detail: "Only a sent estimate can be accepted."),
            AcceptEstimateOutcome.Incomplete => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Estimate cannot be accepted.",
                detail: "An estimate must include at least one room before it can be accepted."),
            AcceptEstimateOutcome.AlreadyAccepted => Ok(result.Job),
            AcceptEstimateOutcome.Created => CreatedAtAction(
                nameof(JobsController.Get),
                "Jobs",
                new { id = result.Job!.Id },
                result.Job),
            _ => throw new InvalidOperationException($"Unexpected accept outcome {result.Outcome}.")
        };
    }
}