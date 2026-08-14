using FlooringManager.Application.Auth;
using FlooringManager.Application.Estimates;
using FlooringManager.Domain.Estimates;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlooringManager.Infrastructure.Estimates;

public sealed class EstimateService(
    ApplicationDbContext db,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider) : IEstimateService
{
    public async Task<EstimateResponse?> CreateAsync(
        CreateEstimateRequest request, CancellationToken ct)
    {
        var user = await RequireUserAsync(ct);

        var (customerOk, propertyOk) = await ValidateOwnershipAsync(
            user.CompanyId, request.CustomerId, request.PropertyId, ct);
        if (!customerOk || !propertyOk) return null;

        var now = timeProvider.GetUtcNow();
        var estimate = new Estimate
        {
            Id = Guid.NewGuid(),
            CompanyId = user.CompanyId,
            CustomerId = request.CustomerId,
            PropertyId = request.PropertyId,
            EstimateNumber = await AllocateEstimateNumberAsync(user.CompanyId, ct),
            Status = EstimateStatus.Draft,
            CreatedDate = now,
            ExpirationDate = request.ExpirationDate,
            TaxRate = request.TaxRate,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            UpdatedAt = now
        };

        ApplyRooms(estimate, request.Rooms);
        RecalculateTotals(estimate);

        db.Estimates.Add(estimate);
        await db.SaveChangesAsync(ct);

        return await LoadResponseAsync(estimate.Id, ct);
    }

    public async Task<EstimateResponse?> UpdateAsync(
        Guid id, UpdateEstimateRequest request, CancellationToken ct)
    {
        var user = await RequireUserAsync(ct);

        var estimate = await db.Estimates
            .Include(e => e.Rooms)
            .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == user.CompanyId, ct);

        if (estimate is null) return null;

        if (estimate.Status != EstimateStatus.Draft) return null;

        var (customerOk, propertyOk) = await ValidateOwnershipAsync(
            user.CompanyId, request.CustomerId, request.PropertyId, ct);
        if (!customerOk || !propertyOk) return null;

        estimate.CustomerId = request.CustomerId;
        estimate.PropertyId = request.PropertyId;
        estimate.ExpirationDate = request.ExpirationDate;
        estimate.TaxRate = request.TaxRate;
        estimate.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        estimate.UpdatedAt = timeProvider.GetUtcNow();

        UpsertRooms(estimate, request.Rooms);
        RecalculateTotals(estimate);

        await db.SaveChangesAsync(ct);
        return await LoadResponseAsync(estimate.Id, ct);
    }

    public async Task<EstimateResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await RequireUserAsync(ct);
        return await LoadResponseAsync(id, ct, scopeToCompany: user.CompanyId);
    }

    public async Task<EstimateListResponse> ListAsync(
        int page, int pageSize, EstimateStatus? status, CancellationToken ct)
    {
        var user = await RequireUserAsync(ct);
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 25 : pageSize;

        var q = db.Estimates
            .AsNoTracking()
            .Where(e => e.CompanyId == user.CompanyId);

        if (status is not null) q = q.Where(e => e.Status == status);

        var total = await q.CountAsync(ct);

        var items = await q
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

    private async Task<(bool customerOk, bool propertyOk)> ValidateOwnershipAsync(
        Guid companyId, Guid customerId, Guid propertyId, CancellationToken ct)
    {
        var customerOk = await db.Customers.AnyAsync(
            c => c.Id == customerId && c.CompanyId == companyId, ct);
        var propertyOk = await db.Properties.AnyAsync(
            p => p.Id == propertyId
                 && p.CustomerId == customerId
                 && p.Customer.CompanyId == companyId, ct);
        return (customerOk, propertyOk);
    }

    private async Task<string> AllocateEstimateNumberAsync(Guid companyId, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var count = await db.Estimates.CountAsync(e => e.CompanyId == companyId, ct);
            var candidate = $"EST-{count + 1 + attempt:D4}";
            var taken = await db.Estimates.AnyAsync(
                e => e.CompanyId == companyId && e.EstimateNumber == candidate, ct);
            if (!taken) return candidate;
        }

        return $"EST-{Guid.NewGuid():N}"[..12];
    }

    private static void ApplyRooms(Estimate estimate, List<EstimateRoomInput> inputs)
    {
        var position = 0;
        foreach (var input in inputs)
        {
            var m = new RoomMeasurement(input.LengthFeet, input.WidthFeet, input.WastePercentage);
            _ = new RoomPricing(m.BillableSquareFeet, input.LaborRatePerSqFt, input.MaterialRatePerSqFt);

            estimate.Rooms.Add(new EstimateRoom
            {
                Id = Guid.NewGuid(),
                EstimateId = estimate.Id,
                Name = input.Name.Trim(),
                LengthFeet = m.LengthFeet,
                WidthFeet = m.WidthFeet,
                WastePercentage = m.WastePercentage,
                SquareFeet = m.AreaSquareFeet,
                BillableSquareFeet = m.BillableSquareFeet,
                FlooringType = input.FlooringType,
                WorkType = input.WorkType,
                LaborRatePerSqFt = input.LaborRatePerSqFt,
                MaterialRatePerSqFt = input.MaterialRatePerSqFt,
                Position = position++
            });
        }
    }

    private void UpsertRooms(Estimate estimate, List<EstimateRoomInput> inputs)
    {
        var byId = estimate.Rooms.ToDictionary(r => r.Id);
        var incomingIds = new HashSet<Guid>(inputs.Where(i => i.Id is not null).Select(i => i.Id!.Value));

        foreach (var toDelete in estimate.Rooms.Where(r => !incomingIds.Contains(r.Id)).ToList())
        {
            estimate.Rooms.Remove(toDelete);
            db.EstimateRooms.Remove(toDelete);
        }

        var position = 0;
        foreach (var input in inputs)
        {
            var m = new RoomMeasurement(input.LengthFeet, input.WidthFeet, input.WastePercentage);
            _ = new RoomPricing(m.BillableSquareFeet, input.LaborRatePerSqFt, input.MaterialRatePerSqFt);

            if (input.Id is Guid rid && byId.TryGetValue(rid, out var room))
            {
                room.Name = input.Name.Trim();
                room.LengthFeet = m.LengthFeet;
                room.WidthFeet = m.WidthFeet;
                room.WastePercentage = m.WastePercentage;
                room.SquareFeet = m.AreaSquareFeet;
                room.BillableSquareFeet = m.BillableSquareFeet;
                room.FlooringType = input.FlooringType;
                room.WorkType = input.WorkType;
                room.LaborRatePerSqFt = input.LaborRatePerSqFt;
                room.MaterialRatePerSqFt = input.MaterialRatePerSqFt;
                room.Position = position++;
            }
            else
            {
                var newRoom = new EstimateRoom
                {
                    Id = Guid.NewGuid(),
                    EstimateId = estimate.Id,
                    Estimate = estimate,
                    Name = input.Name.Trim(),
                    LengthFeet = m.LengthFeet,
                    WidthFeet = m.WidthFeet,
                    WastePercentage = m.WastePercentage,
                    SquareFeet = m.AreaSquareFeet,
                    BillableSquareFeet = m.BillableSquareFeet,
                    FlooringType = input.FlooringType,
                    WorkType = input.WorkType,
                    LaborRatePerSqFt = input.LaborRatePerSqFt,
                    MaterialRatePerSqFt = input.MaterialRatePerSqFt,
                    Position = position++
                };
                estimate.Rooms.Add(newRoom);
                db.EstimateRooms.Add(newRoom);
            }
        }
    }

    private static void RecalculateTotals(Estimate estimate)
    {
        var roomPricings = estimate.Rooms.Select(r =>
            new RoomPricing(r.BillableSquareFeet, r.LaborRatePerSqFt, r.MaterialRatePerSqFt));

        var pricing = EstimatePricing.Calculate(roomPricings, estimate.TaxRate);

        estimate.LaborSubtotal = pricing.LaborSubtotal;
        estimate.MaterialSubtotal = pricing.MaterialSubtotal;
        estimate.Tax = pricing.Tax;
        estimate.Total = pricing.Total;
    }

    private async Task<EstimateResponse?> LoadResponseAsync(
        Guid id, CancellationToken ct, Guid? scopeToCompany = null)
    {
        var e = await db.Estimates
            .AsNoTracking()
            .Include(x => x.Rooms.OrderBy(r => r.Position))
            .FirstOrDefaultAsync(x => x.Id == id
                && (scopeToCompany == null || x.CompanyId == scopeToCompany), ct);

        if (e is null) return null;

        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == e.CustomerId)
            .Select(c => new { c.FirstName, c.LastName })
            .FirstAsync(ct);

        var property = await db.Properties.AsNoTracking()
            .Where(p => p.Id == e.PropertyId)
            .Select(p => new { p.StreetAddress, p.City, p.State, p.PostalCode })
            .FirstAsync(ct);

        return new EstimateResponse(
            e.Id,
            e.EstimateNumber,
            e.Status,
            e.CustomerId,
            $"{customer.FirstName} {customer.LastName}",
            e.PropertyId,
            $"{property.StreetAddress}, {property.City}, {property.State} {property.PostalCode}",
            e.CreatedDate,
            e.ExpirationDate,
            e.UpdatedAt,
            e.LaborSubtotal,
            e.MaterialSubtotal,
            e.TaxRate,
            e.Tax,
            e.LaborSubtotal + e.MaterialSubtotal,
            e.Total,
            e.Notes,
            e.Rooms.Select(r =>
            {
                var pricing = new RoomPricing(r.BillableSquareFeet, r.LaborRatePerSqFt, r.MaterialRatePerSqFt);
                return new EstimateRoomResponse(
                    r.Id, r.Name, r.LengthFeet, r.WidthFeet, r.WastePercentage,
                    r.SquareFeet, r.BillableSquareFeet, r.FlooringType, r.WorkType,
                    r.LaborRatePerSqFt, r.MaterialRatePerSqFt,
                    pricing.LaborCost, pricing.MaterialCost, pricing.RoomTotal,
                    r.Position);
            }).ToList());
    }

    private async Task<CurrentUser> RequireUserAsync(CancellationToken ct) =>
        await currentUserService.GetAsync(ct)
            ?? throw new UnauthorizedAccessException("No provisioned user for the current token.");
}