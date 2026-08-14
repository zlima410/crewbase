namespace FlooringManager.Domain.Estimates;

public sealed class Estimate
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PropertyId { get; set; }

    public string EstimateNumber { get; set; } = string.Empty;
    public EstimateStatus Status { get; set; } = EstimateStatus.Draft;

    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }

    public decimal LaborSubtotal { get; set; }
    public decimal MaterialSubtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<EstimateRoom> Rooms { get; set; } = new List<EstimateRoom>();
}