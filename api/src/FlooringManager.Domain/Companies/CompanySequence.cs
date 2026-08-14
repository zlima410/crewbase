namespace FlooringManager.Domain.Companies;

/// <summary>
/// Per-company counter backing human-readable document numbers (EST-0001,
/// JOB-0001, INV-0001). Keyed by (CompanyId, Prefix) so each company numbers each
/// document family independently, starting at 1.
/// </summary>
public sealed class CompanySequence
{
    public Guid CompanyId { get; set; }

    /// <summary>Document family, e.g. "EST", "JOB", "INV".</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Highest number handed out so far. 0 means nothing allocated yet.</summary>
    public int LastValue { get; set; }
}
