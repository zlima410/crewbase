using FlooringManager.Application.Jobs;

namespace FlooringManager.Application.Estimates;

public enum AcceptEstimateOutcome
{
    NotFound,
    InvalidStatus,
    Incomplete,
    Created,
    AlreadyAccepted
}

public sealed record AcceptEstimateResult(AcceptEstimateOutcome Outcome, JobResponse? Job)
{
    public static AcceptEstimateResult NotFound() => new(AcceptEstimateOutcome.NotFound, null);
    public static AcceptEstimateResult InvalidStatus() => new(AcceptEstimateOutcome.InvalidStatus, null);
    public static AcceptEstimateResult Incomplete() => new(AcceptEstimateOutcome.Incomplete, null);
    public static AcceptEstimateResult Created(JobResponse job) => new(AcceptEstimateOutcome.Created, job);
    public static AcceptEstimateResult AlreadyAccepted(JobResponse job) => new(AcceptEstimateOutcome.AlreadyAccepted, job);
}

public interface IEstimateAcceptanceService
{
    Task<AcceptEstimateResult> AcceptAsync(Guid estimateId, CancellationToken ct);
}
