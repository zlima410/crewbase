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

    public decimal LaborRatePerSqFt { get; set; }
    public decimal MaterialRatePerSqFt { get; set; }

    public int Position { get; set; }
}