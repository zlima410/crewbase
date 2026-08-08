using FlooringManager.Domain.Estimates;

namespace FlooringManager.UnitTests.Estimates;

public sealed class EstimatePricingTests
{
    [Fact]
    public void SingleRoom_Subtotal_MatchesRoomTotal()
    {
        var room = new RoomPricing(billableSquareFeet: 330m, laborRatePerSqFt: 4m, materialRatePerSqFt: 1.25m);
        var pricing = EstimatePricing.Calculate(new[] { room }, tax: 0m);

        Assert.Equal(1320m, pricing.LaborSubtotal);
        Assert.Equal(412.5m, pricing.MaterialSubtotal);
        Assert.Equal(1732.5m, pricing.Subtotal);
        Assert.Equal(1732.5m, pricing.Total);
        Assert.Equal(0m, pricing.Tax);
    }

    [Fact]
    public void MultipleRooms_SumsPerCategory()
    {
        var rooms = new[]
        {
            new RoomPricing(300m, 4m, 1m),    // labor 1200, material 300, total 1500
            new RoomPricing(150m, 5m, 2m),    // labor 750,  material 300, total 1050
            new RoomPricing(80m,  6m, 0.5m),  // labor 480,  material 40,  total 520
        };

        var pricing = EstimatePricing.Calculate(rooms, tax: 100m);

        Assert.Equal(2430m, pricing.LaborSubtotal);      // 1200+750+480
        Assert.Equal(640m, pricing.MaterialSubtotal);    // 300+300+40
        Assert.Equal(3070m, pricing.Subtotal);
        Assert.Equal(100m, pricing.Tax);
        Assert.Equal(3170m, pricing.Total);
    }

    [Fact]
    public void EmptyRooms_ProduceZeroSubtotals()
    {
        var pricing = EstimatePricing.Calculate(Array.Empty<RoomPricing>(), tax: 0m);

        Assert.Equal(0m, pricing.LaborSubtotal);
        Assert.Equal(0m, pricing.MaterialSubtotal);
        Assert.Equal(0m, pricing.Subtotal);
        Assert.Equal(0m, pricing.Total);
    }

    [Fact]
    public void Empty_Helper_Matches_ExplicitEmpty()
    {
        var a = EstimatePricing.Empty(tax: 12.34m);
        var b = EstimatePricing.Calculate(Array.Empty<RoomPricing>(), tax: 12.34m);

        Assert.Equal(a, b);
        Assert.Equal(12.34m, a.Total);
    }

    [Fact]
    public void Tax_IsAddedToSubtotal_ProducingTotal()
    {
        var rooms = new[] { new RoomPricing(100m, 5m, 2m) };
        var pricing = EstimatePricing.Calculate(rooms, tax: 56.75m);

        Assert.Equal(700m, pricing.Subtotal);
        Assert.Equal(56.75m, pricing.Tax);
        Assert.Equal(756.75m, pricing.Total);
    }

    [Fact]
    public void Negative_Tax_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EstimatePricing.Calculate(Array.Empty<RoomPricing>(), tax: -0.01m));
    }

    [Fact]
    public void Null_Rooms_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => EstimatePricing.Calculate(null!, tax: 0m));
    }

    [Fact]
    public void DecimalArithmetic_HasNoFloatingPointDrift()
    {
        var rooms = new[]
        {
            new RoomPricing(1m, 0.1m, 0m),
            new RoomPricing(1m, 0.2m, 0m),
            new RoomPricing(1m, 0.3m, 0m),
        };

        var pricing = EstimatePricing.Calculate(rooms, tax: 0m);
        Assert.Equal(0.6m, pricing.LaborSubtotal);
        Assert.Equal(0.6m, pricing.Total);
    }

    [Fact]
    public void SameInputs_ProduceSamePricing()
    {
        var rooms = new[] { new RoomPricing(200m, 4m, 1m) };
        var a = EstimatePricing.Calculate(rooms, tax: 25m);
        var b = EstimatePricing.Calculate(rooms, tax: 25m);

        Assert.Equal(a, b);
    }

    [Fact]
    public void EndToEnd_MeasurementToPricing_DocExample()
    {
        // Living room 20×15 @ 10% waste → billable 330 (with ceiling area)
        var living = new RoomMeasurement(20m, 15m, 10m);
        // Hallway 15.5×4 @ 5% → ceil(16)×ceil(4)=64, *1.05 = 67.2
        var hallway = new RoomMeasurement(15.5m, 4m, 5m);

        var rooms = new[]
        {
            new RoomPricing(living.BillableSquareFeet, 4m, 1.25m),
            new RoomPricing(hallway.BillableSquareFeet, 5m, 1.5m),
        };

        var pricing = EstimatePricing.Calculate(rooms, tax: 50m);

        // Living: 1320 labor, 412.5 material
        // Hallway: 67.2 × 5 = 336 labor, 67.2 × 1.5 = 100.8 material
        Assert.Equal(1656m, pricing.LaborSubtotal);      // 1320 + 336
        Assert.Equal(513.3m, pricing.MaterialSubtotal);  // 412.5 + 100.8
        Assert.Equal(2169.3m, pricing.Subtotal);
        Assert.Equal(2219.3m, pricing.Total);
    }
}