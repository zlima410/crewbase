namespace FlooringManager.Application.Customers;
public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<CustomerResponse?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerListResponse> SearchAsync(CustomerSearchQuery query, CancellationToken cancellationToken);
}