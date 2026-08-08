using FlooringManager.Domain.Estimates;

namespace FlooringManager.UnitTests.Estimates;

public sealed class RoomPricingTests
{
    [Theory]
    [InlineData(330, 4, 1.25, 1320, 412.5, 1732.5)]
    [InlineData(100, 5, 2, 500, 200, 700)]
    [InlineData(0, 5, 2, 0, 0, 0)]
    [InlineData(200, 0, 0, 0, 0, 0)]
    [InlineData(200, 10, 0, 2000, 0, 2000)]
    [InlineData(200, 0, 3, 0, 600, 600)]
    public void Formulas_Match(
        decimal billable,
        decimal labor,
        decimal material,
        decimal expectedLabor,
        decimal expectedMaterial,
        decimal expectedTotal)
    {
        var p = new RoomPricing(billable, labor, material);

        Assert.Equal(expectedLabor, p.LaborCost);
        Assert.Equal(expectedMaterial, p.MaterialCost);
        Assert.Equal(expectedTotal, p.RoomTotal);
    }

    [Fact]
    public void ComposesWithRoomMeasurement_DocExample()
    {
        var m = new RoomMeasurement(20m, 15m, 10m);
        var p = new RoomPricing(m.BillableSquareFeet, laborRatePerSqFt: 4m, materialRatePerSqFt: 1.25m);

        Assert.Equal(330m, p.BillableSquareFeet);
        Assert.Equal(1320m, p.LaborCost);
        Assert.Equal(412.5m, p.MaterialCost);
        Assert.Equal(1732.5m, p.RoomTotal);
    }

    [Fact]
    public void DecimalArithmetic_IsExact()
    {
        var p = new RoomPricing(100m, 0.1m, 0.2m);
        Assert.Equal(10m, p.LaborCost);
        Assert.Equal(20m, p.MaterialCost);
        Assert.Equal(30m, p.RoomTotal);
    }

    [Fact]
    public void SameInputs_ProduceSameOutputs_AndAreEqual()
    {
        var a = new RoomPricing(123.45m, 4.5m, 1.75m);
        var b = new RoomPricing(123.45m, 4.5m, 1.75m);

        Assert.Equal(a, b);
        Assert.Equal(a.LaborCost, b.LaborCost);
        Assert.Equal(a.MaterialCost, b.MaterialCost);
        Assert.Equal(a.RoomTotal, b.RoomTotal);
    }

    [Fact]
    public void Negative_Billable_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RoomPricing(-0.01m, 4m, 1m));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    [InlineData(10_000.01)]
    public void Invalid_LaborRate_Throws(decimal rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RoomPricing(100m, rate, 1m));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    [InlineData(10_000.01)]
    public void Invalid_MaterialRate_Throws(decimal rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RoomPricing(100m, 1m, rate));
    }

    [Fact]
    public void TryCreate_Valid_ReturnsTrue()
    {
        var ok = RoomPricing.TryCreate(100m, 4m, 1m, out var p, out var err);

        Assert.True(ok);
        Assert.Null(err);
        Assert.Equal(500m, p.RoomTotal);
    }

    [Fact]
    public void TryCreate_Invalid_ReturnsFalseAndSurfacesError()
    {
        var ok = RoomPricing.TryCreate(100m, -1m, 1m, out _, out var err);

        Assert.False(ok);
        Assert.NotNull(err);
        Assert.Contains("Labor", err!);
    }
}