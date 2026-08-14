using System.Data;
using FlooringManager.Application.Auth;
using FlooringManager.Application.Properties;
using FlooringManager.Domain.Properties;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;

namespace FlooringManager.Infrastructure.Properties;

public sealed class PropertyService(ApplicationDbContext db, ICurrentUserService currentUserService, TimeProvider timeProvider) : IPropertyService
{
    public async Task<PropertyResponse?> CreateForCustomerAsync(Guid customerId, CreatePropertyRequest request, CancellationToken ct)
    {
        var user = await RequireUserAsync(ct);

        var customerExists = await db.Customers
            .AsNoTracking()
            .AnyAsync(c => c.Id == customerId && c.CompanyId == user.CompanyId, ct);

        if (!customerExists) return null;

        var now = timeProvider.GetUtcNow();
        var property = new Property
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            StreetAddress = request.StreetAddress.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            PostalCode = request.PostalCode.Trim(),
            AccessNotes = string.IsNullOrWhiteSpace(request.AccessNotes) ? null : request.AccessNotes.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Properties.Add(property);
        await db.SaveChangesAsync(ct);

        return ToResponse(property);
    }

    public async Task<PropertyResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await RequireUserAsync(ct);

        return await db.Properties
            .AsNoTracking()
            .Where(p => p.Id == id && p.Customer.CompanyId == user.CompanyId)
            .Select(p => new PropertyResponse(p.Id, p.CustomerId, p.StreetAddress, p.City, p.State, p.PostalCode, p.AccessNotes, p.CreatedAt, p.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<PropertyResponse>> ListForCustomerAsync(Guid customerId, CancellationToken ct)
    {
        var user = await RequireUserAsync(ct);

        return await db.Properties
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId && p.Customer.CompanyId == user.CompanyId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PropertyResponse(
                p.Id, p.CustomerId, p.StreetAddress, p.City, p.State,
                p.PostalCode, p.AccessNotes, p.CreatedAt, p.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<PropertyResponse?> UpdateAsync(Guid id, UpdatePropertyRequest request, CancellationToken ct)
    {
        var user = await RequireUserAsync(ct);

        var property = await db.Properties
            .Where(p => p.Id == id && p.Customer.CompanyId == user.CompanyId)
            .FirstOrDefaultAsync(ct);

        if (property is null) return null;

        property.StreetAddress = request.StreetAddress.Trim();
        property.City = request.City.Trim();
        property.State = request.State.Trim();
        property.PostalCode = request.PostalCode.Trim();
        property.AccessNotes = string.IsNullOrWhiteSpace(request.AccessNotes) ? null : request.AccessNotes.Trim();
        property.UpdatedAt = timeProvider.GetUtcNow();
        
        await db.SaveChangesAsync(ct);
        return ToResponse(property);
    }

    private async Task<CurrentUser> RequireUserAsync(CancellationToken ct) =>
        await currentUserService.GetAsync(ct) ?? throw new UnauthorizedAccessException("No provisioned user for the current token.");

    private static PropertyResponse ToResponse(Property p) => new(p.Id, p.CustomerId, p.StreetAddress, p.City, p.State, p.PostalCode, p.AccessNotes, p.CreatedAt, p.UpdatedAt);
}