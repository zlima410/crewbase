using FlooringManager.Domain.Estimates;

namespace FlooringManager.UnitTests.Estimates;

public sealed class EstimatePricingTests
{
    [Fact]
    public void SingleRoom_Subtotal_MatchesRoomTotal()
    {
        var room = new RoomPricing(billableSquareFeet: 330m, laborRatePerSqFt: 4m, materialRatePerSqFt: 1.25m);
        var pricing = EstimatePricing.Calculate(new[] { room }, taxRate: 0m);

        Assert.Equal(1320m, pricing.LaborSubtotal);
        Assert.Equal(412.5m, pricing.MaterialSubtotal);
        Assert.Equal(1732.5m, pricing.Subtotal);
        Assert.Equal(1732.5m, pricing.Total);
        Assert.Equal(0m, pricing.Tax);
        Assert.Equal(0m, pricing.TaxRate);
    }

    [Fact]
    public void MultipleRooms_SumsPerCategory_AppliesTaxRate()
    {
        var rooms = new[]
        {
            new RoomPricing(300m, 4m, 1m),    // labor 1200, material 300, total 1500
            new RoomPricing(150m, 5m, 2m),    // labor 750,  material 300, total 1050
            new RoomPricing(80m,  6m, 0.5m),  // labor 480,  material 40,  total 520
        };

        // Subtotal 3070; 10% → tax 307; total 3377
        var pricing = EstimatePricing.Calculate(rooms, taxRate: 10m);

        Assert.Equal(2430m, pricing.LaborSubtotal);      // 1200+750+480
        Assert.Equal(640m, pricing.MaterialSubtotal);    // 300+300+40
        Assert.Equal(3070m, pricing.Subtotal);
        Assert.Equal(10m, pricing.TaxRate);
        Assert.Equal(307m, pricing.Tax);
        Assert.Equal(3377m, pricing.Total);
    }

    [Fact]
    public void EmptyRooms_ProduceZeroSubtotals()
    {
        var pricing = EstimatePricing.Calculate(Array.Empty<RoomPricing>(), taxRate: 8.25m);

        Assert.Equal(0m, pricing.LaborSubtotal);
        Assert.Equal(0m, pricing.MaterialSubtotal);
        Assert.Equal(0m, pricing.Subtotal);
        Assert.Equal(8.25m, pricing.TaxRate);
        Assert.Equal(0m, pricing.Tax);
        Assert.Equal(0m, pricing.Total);
    }

    [Fact]
    public void Empty_Helper_Matches_ExplicitEmpty()
    {
        var a = EstimatePricing.Empty(taxRate: 5m);
        var b = EstimatePricing.Calculate(Array.Empty<RoomPricing>(), taxRate: 5m);

        Assert.Equal(a, b);
        Assert.Equal(5m, a.TaxRate);
        Assert.Equal(0m, a.Total);
    }

    [Fact]
    public void Tax_IsAddedToSubtotalTimesRateOver100()
    {
        var rooms = new[] { new RoomPricing(100m, 5m, 2m) };
        var pricing = EstimatePricing.Calculate(rooms, taxRate: 8.25m);

        Assert.Equal(700m, pricing.Subtotal);
        Assert.Equal(8.25m, pricing.TaxRate);
        Assert.Equal(57.75m, pricing.Tax);      // 700 * 0.0825
        Assert.Equal(757.75m, pricing.Total);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Invalid_TaxRate_Throws(decimal taxRate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EstimatePricing.Calculate(Array.Empty<RoomPricing>(), taxRate));
    }

    [Fact]
    public void Null_Rooms_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => EstimatePricing.Calculate(null!, taxRate: 0m));
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

        var pricing = EstimatePricing.Calculate(rooms, taxRate: 0m);
        Assert.Equal(0.6m, pricing.LaborSubtotal);
        Assert.Equal(0.6m, pricing.Total);
    }

    [Fact]
    public void SameInputs_ProduceSamePricing()
    {
        var rooms = new[] { new RoomPricing(200m, 4m, 1m) };
        var a = EstimatePricing.Calculate(rooms, taxRate: 7.5m);
        var b = EstimatePricing.Calculate(rooms, taxRate: 7.5m);

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

        var pricing = EstimatePricing.Calculate(rooms, taxRate: 10m);

        // Living: 1320 labor, 412.5 material
        // Hallway: 67.2 × 5 = 336 labor, 67.2 × 1.5 = 100.8 material
        // Subtotal 2169.3; 10% → tax 216.93; total 2386.23
        Assert.Equal(1656m, pricing.LaborSubtotal);      // 1320 + 336
        Assert.Equal(513.3m, pricing.MaterialSubtotal);  // 412.5 + 100.8
        Assert.Equal(2169.3m, pricing.Subtotal);
        Assert.Equal(10m, pricing.TaxRate);
        Assert.Equal(216.93m, pricing.Tax);
        Assert.Equal(2386.23m, pricing.Total);
    }
}
