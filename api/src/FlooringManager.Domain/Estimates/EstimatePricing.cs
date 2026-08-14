namespace FlooringManager.Domain.Estimates;

/// <summary>
/// Estimate-level totals derived from a set of RoomPricing and a tax rate (%).
/// </summary>
///
/// <remarks>
/// Formulas:
///   LaborSubtotal    = Sum(room.LaborCost)
///   MaterialSubtotal = Sum(room.MaterialCost)
///   Subtotal         = LaborSubtotal + MaterialSubtotal
///   Tax              = Subtotal × (TaxRate / 100)
///   Total            = Subtotal + Tax
///
/// Bounds:
///   TaxRate: [0, 100]
/// </remarks>
public readonly record struct EstimatePricing
{
    public const decimal MinTaxRate = 0m;
    public const decimal MaxTaxRate = 100m;

    public decimal LaborSubtotal { get; }
    public decimal MaterialSubtotal { get; }
    public decimal TaxRate { get; }
    public decimal Tax { get; }

    private EstimatePricing(decimal laborSubtotal, decimal materialSubtotal, decimal taxRate, decimal tax)
    {
        LaborSubtotal = laborSubtotal;
        MaterialSubtotal = materialSubtotal;
        TaxRate = taxRate;
        Tax = tax;
    }

    public decimal Subtotal => LaborSubtotal + MaterialSubtotal;
    public decimal Total => Subtotal + Tax;

    public static EstimatePricing Calculate(IEnumerable<RoomPricing> rooms, decimal taxRate)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        if (taxRate < MinTaxRate || taxRate > MaxTaxRate)
            throw new ArgumentOutOfRangeException(
                nameof(taxRate),
                taxRate,
                $"Tax rate must be in [{MinTaxRate}, {MaxTaxRate}].");

        decimal labor = 0m;
        decimal material = 0m;

        foreach (var room in rooms)
        {
            labor += room.LaborCost;
            material += room.MaterialCost;
        }

        var subtotal = labor + material;
        var tax = subtotal * (taxRate / 100m);

        return new EstimatePricing(labor, material, taxRate, tax);
    }

    public static EstimatePricing Empty(decimal taxRate = 0m) =>
        Calculate(Array.Empty<RoomPricing>(), taxRate);
}