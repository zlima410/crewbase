namespace FlooringManager.Application.Properties;

public interface IPropertyService
{
    Task<PropertyResponse?> CreateForCustomerAsync(Guid customerId, CreatePropertyRequest request, CancellationToken ct);
    Task<PropertyResponse?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PropertyResponse>> ListForCustomerAsync(Guid customerId, CancellationToken ct);
    Task<PropertyResponse?> UpdateAsync(Guid id, UpdatePropertyRequest request, CancellationToken ct);
}