using FlooringManager.Domain.Estimates;

namespace FlooringManager.UnitTests.Estimates;

public sealed class RoomMeasurementTests
{
    [Fact]
    public void DocumentedExample_20x15_At10Percent_ProducesArea300_Billable330()
    {
        var m = new RoomMeasurement(lengthFeet: 20m, widthFeet: 15m, wastePercentage: 10m);

        Assert.Equal(300m, m.AreaSquareFeet);
        Assert.Equal(330m, m.BillableSquareFeet);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(10, 10, 100)]
    [InlineData(20, 15, 300)]
    [InlineData(12.5, 10, 130)]
    [InlineData(0.5, 0.5, 1)]
    [InlineData(1000, 1000, 1_000_000)]
    public void Area_IsLengthTimesWidth(decimal length, decimal width, decimal expected)
    {
        var m = new RoomMeasurement(length, width, 0m);
        Assert.Equal(expected, m.AreaSquareFeet);
    }

    [Theory]
    [InlineData(20, 15, 0, 300)]
    [InlineData(20, 15, 5, 315)]
    [InlineData(20, 15, 10, 330)]
    [InlineData(20, 15, 20, 360)]
    [InlineData(20, 15, 100, 600)]
    [InlineData(10.25, 8, 12.5, 99)]
    public void BillableArea_AppliesWasteMultiplier(
        decimal length, decimal width, decimal waste, decimal expected)
    {
        var m = new RoomMeasurement(length, width, waste);
        Assert.Equal(expected, m.BillableSquareFeet);
    }

    [Fact]
    public void SameInputs_ProduceSameOutputs()
    {
        var a = new RoomMeasurement(12.3m, 45.6m, 7.5m);
        var b = new RoomMeasurement(12.3m, 45.6m, 7.5m);

        Assert.Equal(a.AreaSquareFeet, b.AreaSquareFeet);
        Assert.Equal(a.BillableSquareFeet, b.BillableSquareFeet);
        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.0001)]
    [InlineData(1001)]
    public void Invalid_Length_Throws(decimal length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RoomMeasurement(length, 10m, 10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1001)]
    public void Invalid_Width_Throws(decimal width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RoomMeasurement(10m, width, 10m));
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(-10)]
    [InlineData(100.0001)]
    [InlineData(1000)]
    public void Invalid_WastePercentage_Throws(decimal waste)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RoomMeasurement(10m, 10m, waste));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.0001)]
    [InlineData(50)]
    [InlineData(100)]
    public void Valid_WastePercentage_IsAccepted(decimal waste)
    {
        var m = new RoomMeasurement(10m, 10m, waste);
        Assert.Equal(waste, m.WastePercentage);
    }

    [Fact]
    public void TryCreate_Valid_ReturnsTrueAndMeasurement()
    {
        var ok = RoomMeasurement.TryCreate(20m, 15m, 10m, out var m, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(330m, m.BillableSquareFeet);
    }

    [Fact]
    public void TryCreate_Invalid_ReturnsFalseAndError()
    {
        var ok = RoomMeasurement.TryCreate(0m, 15m, 10m, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("Length", error!);
    }

    [Fact]
    public void Constants_MatchDocumentedBounds()
    {
        Assert.Equal(1000m, RoomMeasurement.MaxDimensionFeet);
        Assert.Equal(0m, RoomMeasurement.MinWastePercentage);
        Assert.Equal(100m, RoomMeasurement.MaxWastePercentage);
    }
}