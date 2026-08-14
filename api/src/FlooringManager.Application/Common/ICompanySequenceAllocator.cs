namespace FlooringManager.Application.Common;

/// <summary>
/// Allocates gap-free, per-company, human-readable document numbers.
/// </summary>
public interface ICompanySequenceAllocator
{
    /// <summary>
    /// Reserves and returns the next number for a company and document family,
    /// e.g. <c>EST-0001</c>.
    /// </summary>
    /// <remarks>
    /// Must be called inside the caller's database transaction, alongside the
    /// insert that consumes the number. The allocation takes a row lock that is
    /// held until that transaction commits, which is what serializes concurrent
    /// callers; without a transaction two callers can observe the same value.
    /// </remarks>
    Task<string> NextAsync(Guid companyId, string prefix, CancellationToken cancellationToken = default);
}

/// <summary>Document families that receive human-readable numbers.</summary>
public static class SequencePrefixes
{
    public const string Estimate = "EST";
    public const string Job = "JOB";
    public const string Invoice = "INV";
}
