using AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.FuzzyMatching;

public class LevenshteinCalculatorTests
{
    private readonly LevenshteinCalculator _calculator;

    public LevenshteinCalculatorTests()
    {
        _calculator = new LevenshteinCalculator();
    }

    [Fact]
    public void Calculate_WithIdenticalStrings_ShouldReturn1()
    {
        // Arrange
        var str1 = "Toyota";
        var str2 = "Toyota";

        // Act
        var result = _calculator.Calculate(str1, str2);

        // Assert
        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void Calculate_WithIdenticalStringsDifferentCase_ShouldReturn1()
    {
        // Arrange
        var str1 = "Honda";
        var str2 = "HONDA";

        // Act
        var result = _calculator.Calculate(str1, str2);

        // Assert
        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void Calculate_WithCompletelyDifferentStrings_ShouldReturnLowScore()
    {
        // Arrange
        var str1 = "Toyota";
        var str2 = "BMW";

        // Act
        var result = _calculator.Calculate(str1, str2);

        // Assert
        Assert.True(result < 0.5m);
    }

    [Fact]
    public void Calculate_WithSimilarStrings_ShouldReturnHighScore()
    {
        // Arrange
        var str1 = "Camry";
        var str2 = "Camri";

        // Act
        var result = _calculator.Calculate(str1, str2);

        // Assert
        Assert.True(result >= 0.8m);
    }

    [Fact]
    public void Calculate_WithEmptyStrings_ShouldReturn1()
    {
        // Arrange
        var str1 = string.Empty;
        var str2 = string.Empty;

        // Act
        var result = _calculator.Calculate(str1, str2);

        // Assert
        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void Calculate_WithOneEmptyString_ShouldReturn0()
    {
        // Arrange
        var str1 = "Toyota";
        var str2 = string.Empty;

        // Act
        var result = _calculator.Calculate(str1, str2);

        // Assert
        Assert.Equal(0.0m, result);
    }

    [Fact]
    public void Calculate_WithNullStrings_ShouldReturn1()
    {
        // Arrange
        string? str1 = null;
        string? str2 = null;

        // Act
        var result = _calculator.Calculate(str1!, str2!);

        // Assert
        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void Calculate_WithOneNullString_ShouldReturn0()
    {
        // Arrange
        var str1 = "Toyota";
        string? str2 = null;

        // Act
        var result = _calculator.Calculate(str1, str2!);

        // Assert
        Assert.Equal(0.0m, result);
    }

    [Fact]
    public void Calculate_WithMinorTypo_ShouldReturnHighSimilarity()
    {
        // Arrange
        var str1 = "Chevrolet";
        var str2 = "Chevrolett";

        // Act
        var result = _calculator.Calculate(str1, str2);

        // Assert
        Assert.True(result >= 0.9m);
    }

    [Theory]
    [InlineData("Ford", "Ford", 1.0)]
    [InlineData("ford", "FORD", 1.0)]
    [InlineData("F-150", "F150", 0.8)]
    public void Calculate_WithVariousInputs_ShouldReturnExpectedSimilarity(
        string str1,
        string str2,
        decimal expectedMinSimilarity)
    {
        // Act
        var result = _calculator.Calculate(str1, str2);

        // Assert
        Assert.True(result >= expectedMinSimilarity);
    }
}
