using System.Linq.Expressions;
using FlooringManager.Application.Auth;
using FlooringManager.Application.Jobs;
using FlooringManager.Domain.Jobs;
using FlooringManager.Infrastructure.Common;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlooringManager.Infrastructure.Jobs;

public sealed class JobService(
    ApplicationDbContext db,
    ICurrentUserService currentUserService) : IJobService
{
    public async Task<JobResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await currentUserService.RequireAsync(ct);
        return await LoadAsync(user.CompanyId, job => job.Id == id, ct);
    }

    public async Task<JobResponse?> GetByEstimateIdAsync(Guid estimateId, CancellationToken ct)
    {
        var user = await currentUserService.RequireAsync(ct);
        return await LoadAsync(user.CompanyId, job => job.EstimateId == estimateId, ct);
    }

    private async Task<JobResponse?> LoadAsync(
        Guid companyId,
        Expression<Func<Job, bool>> predicate,
        CancellationToken ct)
    {
        var job = await db.Jobs
            .AsNoTracking()
            .ForCompany(companyId)
            .Include(j => j.Rooms)
            .FirstOrDefaultAsync(predicate, ct);

        if (job is null) return null;

        var customer = await db.Customers
            .AsNoTracking()
            .Where(c => c.Id == job.CustomerId)
            .Select(c => new { c.FirstName, c.LastName })
            .FirstAsync(ct);

        var property = await db.Properties
            .AsNoTracking()
            .Where(p => p.Id == job.PropertyId)
            .Select(p => new { p.StreetAddress, p.City, p.State, p.PostalCode })
            .FirstAsync(ct);

        return JobMapper.ToResponse(
            job,
            $"{customer.FirstName} {customer.LastName}",
            AddressText.Format(property.StreetAddress, property.City, property.State, property.PostalCode));
    }
}
