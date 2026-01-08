using AutomatedMarketIntelligenceTool.Cli;
using FluentAssertions;

namespace AutomatedMarketIntelligenceTool.Cli.Tests;

public class ExitCodesTests
{
    [Fact]
    public void ExitCodes_ShouldHaveCorrectValues()
    {
        // Assert
        ExitCodes.Success.Should().Be(0);
        ExitCodes.GeneralError.Should().Be(1);
        ExitCodes.ValidationError.Should().Be(2);
        ExitCodes.NetworkError.Should().Be(3);
        ExitCodes.DatabaseError.Should().Be(4);
        ExitCodes.ScrapingError.Should().Be(5);
    }

    [Fact]
    public void ExitCodes_ShouldBeUnique()
    {
        // Arrange
        var exitCodes = new[]
        {
            ExitCodes.Success,
            ExitCodes.GeneralError,
            ExitCodes.ValidationError,
            ExitCodes.NetworkError,
            ExitCodes.DatabaseError,
            ExitCodes.ScrapingError
        };

        // Assert
        exitCodes.Should().OnlyHaveUniqueItems();
    }
}
