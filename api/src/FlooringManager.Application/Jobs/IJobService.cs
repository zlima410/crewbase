using FlooringManager.Domain.Jobs;

namespace FlooringManager.Application.Jobs;

public interface IJobService
{
    Task<JobResponse?> GetAsync(Guid id, CancellationToken ct);
    Task<JobResponse?> GetByEstimateIdAsync(Guid estimateId, CancellationToken ct);
    Task<JobListResponse> ListAsync(int page, int pageSize, JobStatus? status, string? search, CancellationToken ct);
}
