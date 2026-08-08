using System.ComponentModel.DataAnnotations;

namespace FlooringManager.Application.Customers;

public sealed record CreateCustomerRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [EmailAddress, MaxLength(320)] string? Email,
    [Required, MaxLength(50)] string Phone,
    [MaxLength(2000)] string? Notes);

public sealed record UpdateCustomerRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [EmailAddress, MaxLength(320)] string? Email,
    [Required, MaxLength(50)] string Phone,
    [MaxLength(2000)] string? Notes);

public sealed record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string Phone,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CustomerListResponse(
    IReadOnlyList<CustomerResponse> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record CustomerSearchQuery(string? Search, int Page = 1, int PageSize = 25);