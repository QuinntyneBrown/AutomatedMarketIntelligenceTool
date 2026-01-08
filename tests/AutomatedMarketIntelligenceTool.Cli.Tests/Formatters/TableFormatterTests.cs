using AutomatedMarketIntelligenceTool.Cli.Formatters;
using FluentAssertions;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Formatters;

public class TableFormatterTests
{
    [Fact]
    public void TableFormatter_ShouldImplementIOutputFormatter()
    {
        // Arrange & Act
        var formatter = new TableFormatter();

        // Assert
        formatter.Should().BeAssignableTo<IOutputFormatter>();
    }

    [Fact]
    public void TableFormatter_CanBeInstantiated()
    {
        // Act
        Action act = () => _ = new TableFormatter();

        // Assert
        act.Should().NotThrow();
    }
}
