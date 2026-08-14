using FlooringManager.Application.Common;
using FlooringManager.Domain.Companies;
using FlooringManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlooringManager.Infrastructure.Common;

/// <summary>
/// Allocates document numbers from the <c>company_sequences</c> table.
/// </summary>
/// <remarks>
/// The increment is a single <c>UPDATE ... SET last_value = last_value + 1</c>,
/// which takes a row lock held until the caller's transaction commits. Concurrent
/// allocations for the same company therefore queue rather than reading the same
/// value, which is what the previous COUNT(*)-based scheme could not guarantee.
/// The unique index on (CompanyId, Number) remains as a last-resort backstop.
/// </remarks>
public sealed class CompanySequenceAllocator(ApplicationDbContext db) : ICompanySequenceAllocator
{
    private const int NumberWidth = 4;

    public async Task<string> NextAsync(
        Guid companyId,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        if (db.ChangeTracker.HasChanges())
            throw new InvalidOperationException(
                "Allocate the document number before adding or modifying entities, "
                    + "so seeding a new counter cannot flush the caller's pending changes.");

        var value = await AllocateAsync(companyId, prefix, cancellationToken)
            ?? await SeedAsync(companyId, prefix, cancellationToken)
            ?? await AllocateAsync(companyId, prefix, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Could not allocate a '{prefix}' number for company {companyId}.");

        return $"{prefix}-{value.ToString($"D{NumberWidth}")}";
    }

    private async Task<int?> AllocateAsync(Guid companyId, string prefix, CancellationToken ct)
    {
        var rows = await db.CompanySequences
            .Where(s => s.CompanyId == companyId && s.Prefix == prefix)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastValue, x => x.LastValue + 1), ct);

        if (rows == 0) return null;

        return await db.CompanySequences
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.Prefix == prefix)
            .Select(s => s.LastValue)
            .FirstAsync(ct);
    }

    private async Task<int?> SeedAsync(Guid companyId, string prefix, CancellationToken ct)
    {
        var sequence = new CompanySequence { CompanyId = companyId, Prefix = prefix, LastValue = 1 };
        db.CompanySequences.Add(sequence);

        try
        {
            await db.SaveChangesAsync(ct);
            return sequence.LastValue;
        }
        catch (DbUpdateException)
        {
            db.Entry(sequence).State = EntityState.Detached;
            return null;
        }
    }
}
