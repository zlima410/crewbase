namespace FlooringManager.Domain.Estimates;

/// <summary>
/// Estimate-level totals derived from a set of RoomPricing and a flat tax
/// amount.
/// </summary>
///
/// <remarks>
/// Formulas:
///   LaborSubtotal    = Sum(room.LaborCost)
///   MaterialSubtotal = Sum(room.MaterialCost)
///   Subtotal         = LaborSubtotal + MaterialSubtotal
///   Total            = Subtotal + Tax
///
/// Tax is a flat dollar amount, not a rate.
///
/// Bounds:
///   Tax:  [0, ∞)   Non-negative.
/// </remarks>
public readonly record struct EstimatePricing
{
    public decimal LaborSubtotal { get; }
    public decimal MaterialSubtotal { get; }
    public decimal Tax { get; }

    private EstimatePricing(decimal laborSubtotal, decimal materialSubtotal, decimal tax)
    {
        LaborSubtotal = laborSubtotal;
        MaterialSubtotal = materialSubtotal;
        Tax = tax;
    }

    public decimal Subtotal => LaborSubtotal + MaterialSubtotal;
    public decimal Total => Subtotal + Tax;

    public static EstimatePricing Calculate(IEnumerable<RoomPricing> rooms, decimal tax)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        if (tax < 0m)
            throw new ArgumentOutOfRangeException(nameof(tax), tax, "Tax must be >= 0.");

        decimal labor = 0m;
        decimal material = 0m;

        foreach (var room in rooms)
        {
            labor += room.LaborCost;
            material += room.MaterialCost;
        }

        return new EstimatePricing(labor, material, tax);
    }

    public static EstimatePricing Empty(decimal tax = 0m) =>
        Calculate(Array.Empty<RoomPricing>(), tax);
}