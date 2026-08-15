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

    public async Task<JobListResponse> ListAsync(
        int page, int pageSize, JobStatus? status, string? search, CancellationToken ct)
    {
        var user = await currentUserService.RequireAsync(ct);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 25 : pageSize;

        var query = db.Jobs
            .AsNoTracking()
            .ForCompany(user.CompanyId);

        if (status is not null) query = query.Where(j => j.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
            query = ApplySearch(query, search.Trim());

        var total = await query.CountAsync(ct);

        var rows = await query
            .Join(db.Customers, j => j.CustomerId, c => c.Id, (j, c) => new { j, c })
            .Join(db.Properties, x => x.j.PropertyId, p => p.Id, (x, p) => new { x.j, x.c, p })
            .OrderBy(x => x.j.Status)
            .ThenBy(x => x.j.ScheduledStart == null)
            .ThenBy(x => x.j.ScheduledStart)
            .ThenByDescending(x => x.j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.j.Id,
                x.j.JobNumber,
                x.j.Status,
                x.j.CustomerId,
                x.c.FirstName,
                x.c.LastName,
                x.p.StreetAddress,
                x.p.City,
                x.p.State,
                x.p.PostalCode,
                x.j.ScheduledStart,
                x.j.ScheduledEnd,
                x.j.CreatedAt
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new JobListItem(
            r.Id,
            r.JobNumber,
            r.Status,
            r.CustomerId,
            $"{r.FirstName} {r.LastName}",
            AddressText.Format(r.StreetAddress, r.City, r.State, r.PostalCode),
            r.ScheduledStart,
            r.ScheduledEnd,
            r.CreatedAt)).ToList();

        return new JobListResponse(items, page, pageSize, total);
    }

    private IQueryable<Job> ApplySearch(IQueryable<Job> query, string search)
    {
        var pattern = $"%{EscapeLike(search)}%";

        if (db.Database.IsNpgsql())
        {
            return query.Where(j =>
                EF.Functions.ILike(j.JobNumber, pattern) ||
                db.Customers.Any(c => c.Id == j.CustomerId && (
                    EF.Functions.ILike(c.FirstName, pattern) ||
                    EF.Functions.ILike(c.LastName, pattern) ||
                    EF.Functions.ILike(c.FirstName + " " + c.LastName, pattern))) ||
                db.Properties.Any(p => p.Id == j.PropertyId && (
                    EF.Functions.ILike(p.StreetAddress, pattern) ||
                    EF.Functions.ILike(p.City, pattern) ||
                    EF.Functions.ILike(p.State, pattern) ||
                    EF.Functions.ILike(p.PostalCode, pattern))));
        }

        return query.Where(j =>
            EF.Functions.Like(j.JobNumber, pattern) ||
            db.Customers.Any(c => c.Id == j.CustomerId && (
                EF.Functions.Like(c.FirstName, pattern) ||
                EF.Functions.Like(c.LastName, pattern) ||
                EF.Functions.Like(c.FirstName + " " + c.LastName, pattern))) ||
            db.Properties.Any(p => p.Id == j.PropertyId && (
                EF.Functions.Like(p.StreetAddress, pattern) ||
                EF.Functions.Like(p.City, pattern) ||
                EF.Functions.Like(p.State, pattern) ||
                EF.Functions.Like(p.PostalCode, pattern))));
    }

    private static string EscapeLike(string s) =>
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

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
            .Select(c => new { c.FirstName, c.LastName, c.Phone, c.Email })
            .FirstAsync(ct);

        var property = await db.Properties
            .AsNoTracking()
            .Where(p => p.Id == job.PropertyId)
            .Select(p => new { p.StreetAddress, p.City, p.State, p.PostalCode, p.AccessNotes })
            .FirstAsync(ct);

        return JobMapper.ToResponse(
            job,
            $"{customer.FirstName} {customer.LastName}",
            customer.Phone,
            customer.Email,
            AddressText.Format(property.StreetAddress, property.City, property.State, property.PostalCode),
            property.AccessNotes);
    }
}
