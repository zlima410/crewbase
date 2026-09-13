using FlooringManager.Domain.Estimates;

namespace FlooringManager.Domain.Jobs;

public sealed class JobRoom
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal SquareFeet { get; set; }
    public decimal BillableSquareFeet { get; set; }

    public FlooringType FlooringType { get; set; } = FlooringType.SolidHardwood;
    public WorkType WorkType { get; set; } = WorkType.NewInstallation;
    public InstallationMethod? InstallationMethod { get; set; }
    public FinishType? FinishType { get; set; }
    public string? Notes { get; set; }

    public int Position { get; set; }

    public static JobRoom CopyFrom(EstimateRoom source, Guid jobId) =>
        new()
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            Name = source.Name,
            SquareFeet = source.SquareFeet,
            BillableSquareFeet = source.BillableSquareFeet,
            FlooringType = source.FlooringType,
            WorkType = source.WorkType,
            InstallationMethod = source.InstallationMethod,
            FinishType = source.FinishType,
            Notes = source.Notes,
            Position = source.Position
        };
}
