using FlooringManager.Application.Auth;
using FlooringManager.Application.Common;
using FlooringManager.Application.Customers;
using FlooringManager.Domain.Customers;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlooringManager.Infrastructure.Customers;

public sealed class CustomerService(ApplicationDbContext db, ICurrentUserService currentUserService, TimeProvider timeProvider) : ICustomerService
{
    private static string EscapeLike(string s) =>
    s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var user = await currentUserService.RequireAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = user.CompanyId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = OptionalText.Normalize(request.Email),
            Phone = request.Phone.Trim(),
            Notes = OptionalText.Normalize(request.Notes),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task<CustomerResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await currentUserService.RequireAsync(cancellationToken);

        return await db.Customers
            .AsNoTracking()
            .ForCompany(user.CompanyId)
            .Where(c => c.Id == id)
            .Select(c => new CustomerResponse(c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.Notes, c.CreatedAt, c.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerResponse?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var user = await currentUserService.RequireAsync(cancellationToken);

        var customer = await db.Customers
            .ForCompany(user.CompanyId)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null) return null;

        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Email = OptionalText.Normalize(request.Email);
        customer.Phone = request.Phone.Trim();
        customer.Notes = OptionalText.Normalize(request.Notes);
        customer.UpdatedAt = timeProvider.GetUtcNow();

        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    public async Task<CustomerListResponse> SearchAsync(CustomerSearchQuery query, CancellationToken cancellationToken)
    {
        var user = await currentUserService.RequireAsync(cancellationToken);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 25 : query.PageSize;

        var q = db.Customers
            .AsNoTracking()
            .ForCompany(user.CompanyId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";

            if (db.Database.IsNpgsql())
            {
                q = q.Where(c =>
                    EF.Functions.ILike(c.FirstName, pattern) ||
                    EF.Functions.ILike(c.LastName, pattern) ||
                    (c.Email != null && EF.Functions.ILike(c.Email, pattern)) ||
                    EF.Functions.ILike(c.Phone, pattern));
            }
            else
            {
                q = q.Where(c =>
                    EF.Functions.Like(c.FirstName, pattern) ||
                    EF.Functions.Like(c.LastName, pattern) ||
                    (c.Email != null && EF.Functions.Like(c.Email, pattern)) ||
                    EF.Functions.Like(c.Phone, pattern));
            }
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

    private static CustomerResponse ToResponse(Customer c) => new(c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.Notes, c.CreatedAt, c.UpdatedAt);
}