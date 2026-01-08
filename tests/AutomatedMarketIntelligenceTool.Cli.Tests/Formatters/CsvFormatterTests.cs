using AutomatedMarketIntelligenceTool.Cli.Formatters;
using FluentAssertions;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Formatters;

public class CsvFormatterTests : ConsoleTestBase
{
    [Fact]
    public void Format_WithValidData_ShouldOutputCsvFormat()
    {
        // Arrange
        var formatter = new CsvFormatter();
        var data = new[]
        {
            new TestObject { Name = "Test1", Value = 100 },
            new TestObject { Name = "Test2", Value = 200 }
        };

        // Act
        formatter.Format(data);

        // Assert
        var output = GetConsoleOutput();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        lines.Should().HaveCount(3); // Header + 2 data rows
        lines[0].Should().Be("Name,Value"); // Header
        lines[1].Should().Be("Test1,100"); // First row
        lines[2].Should().Be("Test2,200"); // Second row
    }

    [Fact]
    public void Format_WithEmptyData_ShouldOutputNothing()
    {
        // Arrange
        var formatter = new CsvFormatter();
        var data = Array.Empty<TestObject>();

        // Act
        formatter.Format(data);

        // Assert
        var output = GetConsoleOutput();
        output.Should().BeEmpty();
    }

    [Fact]
    public void Format_WithCommasInData_ShouldEscapeValues()
    {
        // Arrange
        var formatter = new CsvFormatter();
        var data = new[]
        {
            new TestObject { Name = "Test, with comma", Value = 100 }
        };

        // Act
        formatter.Format(data);

        // Assert
        var output = GetConsoleOutput();
        output.Should().Contain("\"Test, with comma\"");
    }

    [Fact]
    public void Format_WithQuotesInData_ShouldEscapeQuotes()
    {
        // Arrange
        var formatter = new CsvFormatter();
        var data = new[]
        {
            new TestObject { Name = "Test \"quoted\" value", Value = 100 }
        };

        // Act
        formatter.Format(data);

        // Assert
        var output = GetConsoleOutput();
        // Should contain escaped quotes - doubled quotes within quotes
        output.Should().Contain("\"\"");
    }

    [Fact]
    public void Format_WithMultipleProperties_ShouldOutputAllColumns()
    {
        // Arrange
        var formatter = new CsvFormatter();
        var data = new[]
        {
            new ComplexObject
            {
                StringProp = "Test",
                IntProp = 42,
                DecimalProp = 99.99m
            }
        };

        // Act
        formatter.Format(data);

        // Assert
        var output = GetConsoleOutput();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        lines[0].Should().Be("StringProp,IntProp,DecimalProp");
        lines[1].Should().Be("Test,42,99.99");
    }

    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private class ComplexObject
    {
        public string StringProp { get; set; } = string.Empty;
        public int IntProp { get; set; }
        public decimal DecimalProp { get; set; }
    }
}
