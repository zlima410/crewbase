namespace FlooringManager.Domain.Estimates;

public sealed class EstimateRoom
{
    public Guid Id { get; set; }
    public Guid EstimateId { get; set; }
    public Estimate Estimate { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public decimal LengthFeet { get; set; }
    public decimal WidthFeet { get; set; }
    public decimal WastePercentage { get; set; }
    public decimal SquareFeet { get; set; }
    public decimal BillableSquareFeet { get; set; }

    public FlooringType FlooringType { get; set; } = FlooringType.SolidHardwood;
    public WorkType WorkType { get; set; } = WorkType.NewInstallation;

    // Optional: a room can be priced without them, and on a refinishing job the crew
    // may not know either until they are on site. Null means "not recorded".
    public InstallationMethod? InstallationMethod { get; set; }
    public FinishType? FinishType { get; set; }

    // The escape hatch for detail the enums above deliberately do not capture — a
    // specific stain or product line. Keeps a materials catalog out of the MVP.
    public string? Notes { get; set; }

    public decimal LaborRatePerSqFt { get; set; }
    public decimal MaterialRatePerSqFt { get; set; }

    public int Position { get; set; }
}