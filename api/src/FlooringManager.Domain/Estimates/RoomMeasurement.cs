using System.Data;

namespace FlooringManager.Domain.Estimates;

/// <summary>
/// A single room's measurement, expressed in feet, with an associated waste
/// allowance. Computes area and billable area deterministically using decimal
/// arithmetic. Immutable and value-equatable.
/// </summary>
///
/// <remarks>
/// Formulas:
///   Area = LengthFeet * WidthFeet
///   BillableSquareFeet = Area * (1 + WastePercentage / 100)
///
/// Bounds:
///   LengthFeet:       (0, 1000]     Rooms are inclusively bounded at 1,000 ft.
///   WidthFeet:        (0, 1000]     Same bound.
///   WastePercentage:  [0, 100]      Hardwood waste is typically 5-20%;
/// </remarks>
public readonly record struct RoomMeasurement
{
    public const decimal MaxDimensionFeet = 1000m;
    public const decimal MinWastePercentage = 0m;
    public const decimal MaxWastePercentage = 100m;

    public decimal LengthFeet { get; }
    public decimal WidthFeet { get; }
    public decimal WastePercentage { get; }

    public RoomMeasurement(decimal lengthFeet, decimal widthFeet, decimal wastePercentage)
    {
        if (lengthFeet <= 0m || lengthFeet > MaxDimensionFeet)
            throw new ArgumentOutOfRangeException(nameof(lengthFeet), lengthFeet, $"Length must be > 0 and <= {MaxDimensionFeet} ft.");

        if (widthFeet <= 0m || widthFeet > MaxDimensionFeet)
            throw new ArgumentOutOfRangeException(nameof(widthFeet), widthFeet, $"Width must be > 0 and <= {MaxDimensionFeet} ft.");

        if (wastePercentage < MinWastePercentage || wastePercentage > MaxWastePercentage)
            throw new ArgumentOutOfRangeException(nameof(wastePercentage), wastePercentage, $"Waste percentage must be in [{MinWastePercentage}, {MaxWastePercentage}].");

        LengthFeet = lengthFeet;
        WidthFeet = widthFeet;
        WastePercentage = wastePercentage;
    }

    public decimal AreaSquareFeet => Math.Ceiling(LengthFeet) * Math.Ceiling(WidthFeet);

    public decimal BillableSquareFeet => AreaSquareFeet * (1m + WastePercentage / 100m);

    public static bool TryCreate(decimal lengthFeet, decimal widthFeet, decimal wastePercentage, out RoomMeasurement measurement, out string? error)
    {
        try
        {
            measurement = new RoomMeasurement(lengthFeet, widthFeet, wastePercentage);
            error = null;
            return true;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            measurement = default;
            error = ex.Message;
            return false;
        }
    }
}