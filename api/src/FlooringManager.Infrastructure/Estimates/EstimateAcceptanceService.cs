using FlooringManager.Application.Auth;
using FlooringManager.Application.Common;
using FlooringManager.Application.Estimates;
using FlooringManager.Application.Jobs;
using FlooringManager.Domain.Estimates;
using FlooringManager.Domain.Jobs;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlooringManager.Infrastructure.Estimates;

public sealed class EstimateAcceptanceService(
    ApplicationDbContext db,
    ICurrentUserService currentUserService,
    ICompanySequenceAllocator sequences,
    IJobService jobs,
    TimeProvider timeProvider) : IEstimateAcceptanceService
{
    public async Task<AcceptEstimateResult> AcceptAsync(Guid estimateId, CancellationToken ct)
    {
        var user = await currentUserService.RequireAsync(ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var estimate = await db.Estimates
            .ForCompany(user.CompanyId)
            .Include(e => e.Rooms)
            .FirstOrDefaultAsync(e => e.Id == estimateId, ct);

        if (estimate is null)
            return AcceptEstimateResult.NotFound();

        if (estimate.Status == EstimateStatus.Accepted)
        {
            var existing = await jobs.GetByEstimateIdAsync(estimateId, ct);
            return existing is null
                ? AcceptEstimateResult.InvalidStatus()
                : AcceptEstimateResult.AlreadyAccepted(existing);
        }

        if (!EstimateTransitions.CanAccept(estimate.Status))
            return AcceptEstimateResult.InvalidStatus();

        if (estimate.Rooms.Count == 0)
            return AcceptEstimateResult.Incomplete();

        var now = timeProvider.GetUtcNow();
        var jobNumber = await sequences.NextAsync(user.CompanyId, SequencePrefixes.Job, ct);
        var job = Job.FromAcceptedEstimate(estimate, jobNumber, now);

        estimate.Status = EstimateStatus.Accepted;
        estimate.UpdatedAt = now;
        db.Jobs.Add(job);

        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();

            var existing = await jobs.GetByEstimateIdAsync(estimateId, ct);
            if (existing is not null)
                return AcceptEstimateResult.AlreadyAccepted(existing);

            throw;
        }

        var created = await jobs.GetAsync(job.Id, ct)
            ?? throw new InvalidOperationException("Accepted estimate but the job could not be reloaded.");

        return AcceptEstimateResult.Created(created);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
                return true;

            if (string.Equals(
                    inner.GetType().FullName,
                    "Microsoft.Data.Sqlite.SqliteException",
                    StringComparison.Ordinal)
                && inner.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
