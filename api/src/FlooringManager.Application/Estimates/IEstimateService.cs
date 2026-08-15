using FlooringManager.Domain.Estimates;

namespace FlooringManager.Application.Estimates;

public interface IEstimateService
{
    Task<EstimateResponse?> CreateAsync(CreateEstimateRequest request, CancellationToken ct);
    Task<EstimateResponse?> GetAsync(Guid id, CancellationToken ct);
    Task<EstimateResponse?> UpdateAsync(Guid id, UpdateEstimateRequest request, CancellationToken ct);
    Task<EstimateResponse?> SendAsync(Guid id, CancellationToken ct);
    Task<EstimateListResponse> ListAsync(int page, int pageSize, EstimateStatus? status, CancellationToken ct);
}