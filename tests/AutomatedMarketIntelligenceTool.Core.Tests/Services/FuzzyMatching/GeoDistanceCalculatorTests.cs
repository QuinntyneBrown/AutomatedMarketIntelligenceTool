using AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.FuzzyMatching;

public class GeoDistanceCalculatorTests
{
    private readonly GeoDistanceCalculator _calculator;

    public GeoDistanceCalculatorTests()
    {
        _calculator = new GeoDistanceCalculator();
    }

    [Fact]
    public void Calculate_WithIdenticalCoordinates_ShouldReturn1()
    {
        // Arrange
        var lat1 = 43.6532m;
        var lon1 = -79.3832m;
        var lat2 = 43.6532m;
        var lon2 = -79.3832m;
        var toleranceMiles = 10m;

        // Act
        var result = _calculator.Calculate(lat1, lon1, lat2, lon2, toleranceMiles);

        // Assert
        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void Calculate_WithinTolerance_ShouldReturn1()
    {
        // Arrange - Toronto to nearby location (< 10 miles)
        var lat1 = 43.6532m;
        var lon1 = -79.3832m;
        var lat2 = 43.6632m;
        var lon2 = -79.3932m;
        var toleranceMiles = 10m;

        // Act
        var result = _calculator.Calculate(lat1, lon1, lat2, lon2, toleranceMiles);

        // Assert
        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void Calculate_BeyondTolerance_ShouldReturnLessThan1()
    {
        // Arrange - Toronto to Mississauga (> 10 miles)
        var lat1 = 43.6532m;
        var lon1 = -79.3832m;
        var lat2 = 43.5890m;
        var lon2 = -79.6441m;
        var toleranceMiles = 10m;

        // Act
        var result = _calculator.Calculate(lat1, lon1, lat2, lon2, toleranceMiles);

        // Assert
        Assert.True(result < 1.0m);
        Assert.True(result > 0.0m);
    }

    [Fact]
    public void Calculate_WithNullLatitude_ShouldReturn0()
    {
        // Arrange
        decimal? lat1 = null;
        var lon1 = -79.3832m;
        var lat2 = 43.6532m;
        var lon2 = -79.3832m;
        var toleranceMiles = 10m;

        // Act
        var result = _calculator.Calculate(lat1, lon1, lat2, lon2, toleranceMiles);

        // Assert
        Assert.Equal(0.0m, result);
    }

    [Fact]
    public void Calculate_WithNullLongitude_ShouldReturn0()
    {
        // Arrange
        var lat1 = 43.6532m;
        decimal? lon1 = null;
        var lat2 = 43.6532m;
        var lon2 = -79.3832m;
        var toleranceMiles = 10m;

        // Act
        var result = _calculator.Calculate(lat1, lon1, lat2, lon2, toleranceMiles);

        // Assert
        Assert.Equal(0.0m, result);
    }

    [Fact]
    public void Calculate_WithAllNullCoordinates_ShouldReturn0()
    {
        // Arrange
        decimal? lat1 = null;
        decimal? lon1 = null;
        decimal? lat2 = null;
        decimal? lon2 = null;
        var toleranceMiles = 10m;

        // Act
        var result = _calculator.Calculate(lat1, lon1, lat2, lon2, toleranceMiles);

        // Assert
        Assert.Equal(0.0m, result);
    }

    [Fact]
    public void Calculate_WithZeroTolerance_ShouldThrowException()
    {
        // Arrange
        var lat1 = 43.6532m;
        var lon1 = -79.3832m;
        var lat2 = 43.6532m;
        var lon2 = -79.3832m;
        var toleranceMiles = 0m;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _calculator.Calculate(lat1, lon1, lat2, lon2, toleranceMiles));
    }

    [Fact]
    public void Calculate_WithNegativeTolerance_ShouldThrowException()
    {
        // Arrange
        var lat1 = 43.6532m;
        var lon1 = -79.3832m;
        var lat2 = 43.6532m;
        var lon2 = -79.3832m;
        var toleranceMiles = -10m;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _calculator.Calculate(lat1, lon1, lat2, lon2, toleranceMiles));
    }

    [Fact]
    public void Calculate_LongDistance_ShouldCalculateCorrectly()
    {
        // Arrange - Toronto to Montreal (approximately 300+ miles)
        var lat1 = 43.6532m;
        var lon1 = -79.3832m;
        var lat2 = 45.5017m;
        var lon2 = -73.5673m;
        var toleranceMiles = 10m;

        // Act
        var result = _calculator.Calculate(lat1, lon1, lat2, lon2, toleranceMiles);

        // Assert
        Assert.True(result < 0.1m); // Should be very low similarity
        Assert.True(result > 0.0m);
    }
}
