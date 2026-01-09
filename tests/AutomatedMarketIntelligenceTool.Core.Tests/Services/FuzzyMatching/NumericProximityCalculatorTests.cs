using AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.FuzzyMatching;

public class NumericProximityCalculatorTests
{
    private readonly NumericProximityCalculator _calculator;

    public NumericProximityCalculatorTests()
    {
        _calculator = new NumericProximityCalculator();
    }

    [Fact]
    public void Calculate_WithIdenticalValues_ShouldReturn1()
    {
        // Arrange
        var value1 = 25000m;
        var value2 = 25000m;
        var tolerance = 500m;

        // Act
        var result = _calculator.Calculate(value1, value2, tolerance);

        // Assert
        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void Calculate_WithinTolerance_ShouldReturn1()
    {
        // Arrange
        var value1 = 25000m;
        var value2 = 25400m;
        var tolerance = 500m;

        // Act
        var result = _calculator.Calculate(value1, value2, tolerance);

        // Assert
        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void Calculate_ExactlyAtTolerance_ShouldReturn1()
    {
        // Arrange
        var value1 = 25000m;
        var value2 = 25500m;
        var tolerance = 500m;

        // Act
        var result = _calculator.Calculate(value1, value2, tolerance);

        // Assert
        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void Calculate_BeyondTolerance_ShouldReturnLessThan1()
    {
        // Arrange
        var value1 = 25000m;
        var value2 = 26000m;
        var tolerance = 500m;

        // Act
        var result = _calculator.Calculate(value1, value2, tolerance);

        // Assert
        Assert.True(result < 1.0m);
        Assert.True(result > 0.0m);
    }

    [Fact]
    public void Calculate_WithZeroTolerance_ShouldThrowException()
    {
        // Arrange
        var value1 = 25000m;
        var value2 = 25000m;
        var tolerance = 0m;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _calculator.Calculate(value1, value2, tolerance));
    }

    [Fact]
    public void Calculate_WithNegativeTolerance_ShouldThrowException()
    {
        // Arrange
        var value1 = 25000m;
        var value2 = 25000m;
        var tolerance = -500m;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _calculator.Calculate(value1, value2, tolerance));
    }

    [Fact]
    public void CalculateYearSimilarity_WithSameYear_ShouldReturn1()
    {
        // Arrange
        var year1 = 2020;
        var year2 = 2020;

        // Act
        var result = _calculator.CalculateYearSimilarity(year1, year2);

        // Assert
        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void CalculateYearSimilarity_WithOneYearDifference_ShouldReturn08()
    {
        // Arrange
        var year1 = 2020;
        var year2 = 2021;

        // Act
        var result = _calculator.CalculateYearSimilarity(year1, year2);

        // Assert
        Assert.Equal(0.8m, result);
    }

    [Fact]
    public void CalculateYearSimilarity_WithTwoYearDifference_ShouldReturnLessThan08()
    {
        // Arrange
        var year1 = 2020;
        var year2 = 2022;

        // Act
        var result = _calculator.CalculateYearSimilarity(year1, year2);

        // Assert
        Assert.True(result < 0.8m);
        Assert.True(result > 0.0m);
    }

    [Fact]
    public void CalculateYearSimilarity_WithLargeDifference_ShouldNotBeNegative()
    {
        // Arrange
        var year1 = 2020;
        var year2 = 2030;

        // Act
        var result = _calculator.CalculateYearSimilarity(year1, year2);

        // Assert
        Assert.True(result >= 0.0m);
    }

    [Theory]
    [InlineData(100000, 100000, 500, 1.0)]
    [InlineData(100000, 100500, 500, 1.0)]
    [InlineData(100000, 101000, 500, 0.5)]
    public void Calculate_WithMileageValues_ShouldReturnExpectedSimilarity(
        decimal mileage1,
        decimal mileage2,
        decimal tolerance,
        decimal expectedMinSimilarity)
    {
        // Act
        var result = _calculator.Calculate(mileage1, mileage2, tolerance);

        // Assert
        Assert.True(result >= expectedMinSimilarity);
    }
}
