using FlooringManager.Application.Auth;
using FlooringManager.Application.Common;
using FlooringManager.Application.Estimates;
using FlooringManager.Domain.Estimates;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlooringManager.Infrastructure.Estimates;

public sealed class EstimateService(
    ApplicationDbContext db,
    ICurrentUserService currentUserService,
    ICompanySequenceAllocator sequences,
    EstimateRoomSynchronizer rooms,
    TimeProvider timeProvider) : IEstimateService
{
    public async Task<EstimateResponse?> CreateAsync(CreateEstimateRequest request, CancellationToken ct)
    {
        var user = await currentUserService.RequireAsync(ct);

        if (!await OwnsCustomerAndPropertyAsync(user.CompanyId, request.CustomerId, request.PropertyId, ct))
            return null;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var now = timeProvider.GetUtcNow();
        var estimate = new Estimate
        {
            Id = Guid.NewGuid(),
            CompanyId = user.CompanyId,
            CustomerId = request.CustomerId,
            PropertyId = request.PropertyId,
            EstimateNumber = await sequences.NextAsync(user.CompanyId, SequencePrefixes.Estimate, ct),
            Status = EstimateStatus.Draft,
            CreatedDate = now,
            ExpirationDate = request.ExpirationDate,
            TaxRate = request.TaxRate,
            Notes = OptionalText.Normalize(request.Notes),
            UpdatedAt = now
        };

        rooms.Synchronize(estimate, request.Rooms);
        RecalculateTotals(estimate);

        db.Estimates.Add(estimate);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await LoadResponseAsync(estimate.Id, user.CompanyId, ct);
    }

    public async Task<EstimateResponse?> UpdateAsync(Guid id, UpdateEstimateRequest request, CancellationToken ct)
    {
        var user = await currentUserService.RequireAsync(ct);

        var estimate = await db.Estimates
            .ForCompany(user.CompanyId)
            .Include(e => e.Rooms)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (estimate is null) return null;

        if (estimate.Status != EstimateStatus.Draft) return null;

        if (!await OwnsCustomerAndPropertyAsync(user.CompanyId, request.CustomerId, request.PropertyId, ct))
            return null;

        estimate.CustomerId = request.CustomerId;
        estimate.PropertyId = request.PropertyId;
        estimate.ExpirationDate = request.ExpirationDate;
        estimate.TaxRate = request.TaxRate;
        estimate.Notes = OptionalText.Normalize(request.Notes);
        estimate.UpdatedAt = timeProvider.GetUtcNow();

        rooms.Synchronize(estimate, request.Rooms);
        RecalculateTotals(estimate);

        await db.SaveChangesAsync(ct);

        return await LoadResponseAsync(estimate.Id, user.CompanyId, ct);
    }

    public async Task<EstimateResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await currentUserService.RequireAsync(ct);
        return await LoadResponseAsync(id, user.CompanyId, ct);
    }

    public async Task<EstimateListResponse> ListAsync(
        int page, int pageSize, EstimateStatus? status, CancellationToken ct)
    {
        var user = await currentUserService.RequireAsync(ct);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 25 : pageSize;

        var query = db.Estimates
            .AsNoTracking()
            .ForCompany(user.CompanyId);

        if (status is not null) query = query.Where(e => e.Status == status);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EstimateListItem(
                e.Id,
                e.EstimateNumber,
                e.Status,
                e.CustomerId,
                db.Customers.Where(c => c.Id == e.CustomerId)
                    .Select(c => c.FirstName + " " + c.LastName).First(),
                db.Properties.Where(p => p.Id == e.PropertyId)
                    .Select(p => p.StreetAddress).First(),
                e.Total,
                e.CreatedDate))
            .ToListAsync(ct);

        return new EstimateListResponse(items, page, pageSize, total);
    }

    /// <summary>
    /// Confirms the customer belongs to the caller's company and the property belongs
    /// to that customer, so neither id can be borrowed from another tenant or from an
    /// unrelated customer in the same tenant.
    /// </summary>
    private async Task<bool> OwnsCustomerAndPropertyAsync(
        Guid companyId, Guid customerId, Guid propertyId, CancellationToken ct)
    {
        var customerOk = await db.Customers
            .ForCompany(companyId)
            .AnyAsync(c => c.Id == customerId, ct);

        if (!customerOk) return false;

        return await db.Properties
            .ForCompany(companyId)
            .AnyAsync(p => p.Id == propertyId && p.CustomerId == customerId, ct);
    }

    private static void RecalculateTotals(Estimate estimate)
    {
        var pricing = EstimatePricing.Calculate(
            estimate.Rooms.Select(r =>
                new RoomPricing(r.BillableSquareFeet, r.LaborRatePerSqFt, r.MaterialRatePerSqFt)),
            estimate.TaxRate);

        estimate.LaborSubtotal = pricing.LaborSubtotal;
        estimate.MaterialSubtotal = pricing.MaterialSubtotal;
        estimate.Tax = pricing.Tax;
        estimate.Total = pricing.Total;
    }

    private async Task<EstimateResponse?> LoadResponseAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var estimate = await db.Estimates
            .AsNoTracking()
            .ForCompany(companyId)
            .Include(e => e.Rooms.OrderBy(r => r.Position))
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (estimate is null) return null;

        var customer = await db.Customers
            .AsNoTracking()
            .Where(c => c.Id == estimate.CustomerId)
            .Select(c => new { c.FirstName, c.LastName })
            .FirstAsync(ct);

        var property = await db.Properties
            .AsNoTracking()
            .Where(p => p.Id == estimate.PropertyId)
            .Select(p => new { p.StreetAddress, p.City, p.State, p.PostalCode })
            .FirstAsync(ct);

        return EstimateMapper.ToResponse(
            estimate,
            $"{customer.FirstName} {customer.LastName}",
            EstimateMapper.FormatAddress(property.StreetAddress, property.City, property.State, property.PostalCode));
    }

}
