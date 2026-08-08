namespace FlooringManager.Domain.Estimates;

/// <summary>
/// Per-room pricing derived from a billable area and per-sq-ft rates.
/// Immutable, value-equatable, no rounding.
/// </summary>
///
/// <remarks>
/// Formulas:
///   LaborCost    = BillableSquareFeet * LaborRatePerSqFt
///   MaterialCost = BillableSquareFeet * MaterialRatePerSqFt
///   RoomTotal    = LaborCost + MaterialCost
///
/// Bounds:
///   BillableSquareFeet:   [0, ∞)
///   LaborRatePerSqFt:     [0, MaxRate]
///   MaterialRatePerSqFt:  [0, MaxRate]
/// </remarks>
public readonly record struct RoomPricing
{
    public const decimal MaxRate = 10_000m;

    public decimal BillableSquareFeet { get; }
    public decimal LaborRatePerSqFt { get; }
    public decimal MaterialRatePerSqFt { get; }

    public RoomPricing(decimal billableSquareFeet, decimal laborRatePerSqFt, decimal materialRatePerSqFt)
    {
        if (billableSquareFeet < 0m)
            throw new ArgumentOutOfRangeException(nameof(billableSquareFeet), billableSquareFeet, "Billable square feet must be > 0.");

        if (laborRatePerSqFt < 0m || laborRatePerSqFt > MaxRate)
            throw new ArgumentOutOfRangeException(nameof(laborRatePerSqFt), laborRatePerSqFt, $"Labor rate must be in [0, {MaxRate}].");

        if (materialRatePerSqFt < 0m || materialRatePerSqFt > MaxRate)
            throw new ArgumentOutOfRangeException(nameof(materialRatePerSqFt), materialRatePerSqFt, $"Material rate must be in [0, {MaxRate}].");

        BillableSquareFeet = billableSquareFeet;
        LaborRatePerSqFt = laborRatePerSqFt;
        MaterialRatePerSqFt = materialRatePerSqFt;
    }

    public decimal LaborCost => BillableSquareFeet * LaborRatePerSqFt;
    public decimal MaterialCost => BillableSquareFeet * MaterialRatePerSqFt;
    public decimal RoomTotal => LaborCost + MaterialCost;

    public static bool TryCreate(decimal billableSquareFeet, decimal laborRatePerSqFt, decimal materialRatePerSqFt, out RoomPricing pricing, out string? error)
    {
        try
        {
            pricing = new RoomPricing(billableSquareFeet, laborRatePerSqFt, materialRatePerSqFt);
            error = null;
            return true;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            pricing = default;
            error = ex.Message;
            return false;
        }
    }
}