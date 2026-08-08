using FlooringManager.Application.Auth;
using FlooringManager.Application.Customers;
using FlooringManager.Domain.Customers;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace FlooringManager.Infrastructure.Customers;

public sealed class CustomerService(ApplicationDbContext db, ICurrentUserService currentUserService, TimeProvider timeProvider) : ICustomerService
{
    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = user.CompanyId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task<CustomerResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);

        return await db.Customers
            .AsNoTracking()
            .Where(c => c.Id == id && c.CompanyId == user.CompanyId)
            .Select(c => new CustomerResponse(c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.Notes, c.CreatedAt, c.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerResponse?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);

        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == user.CompanyId, cancellationToken);

        if (customer is null) return null;

        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        customer.Phone = request.Phone.Trim();
        customer.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        customer.UpdatedAt = timeProvider.GetUtcNow();

        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task<CustomerListResponse> SearchAsync(CustomerSearchQuery query, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 25 : query.PageSize;

        var q = db.Customers
            .AsNoTracking()
            .Where(c => c.CompanyId == user.CompanyId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(c =>
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                c.Phone.ToLower().Contains(term));
        }

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerResponse(c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.Notes, c.CreatedAt, c.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new CustomerListResponse(items, page, pageSize, total);
    }

    private async Task<CurrentUser> RequireUserAsync(CancellationToken ct)
    {
        var user = await currentUserService.GetAsync(ct);

        return user ?? throw new UnauthorizedAccessException("No provisioned user for the current token.");
    }

    private static CustomerResponse ToResponse(Customer c) => new(c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.Notes, c.CreatedAt, c.UpdatedAt);
}