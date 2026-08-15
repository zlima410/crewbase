namespace FlooringManager.Application.Jobs;

public interface IJobService
{
    Task<JobResponse?> GetAsync(Guid id, CancellationToken ct);
    Task<JobResponse?> GetByEstimateIdAsync(Guid estimateId, CancellationToken ct);
}
