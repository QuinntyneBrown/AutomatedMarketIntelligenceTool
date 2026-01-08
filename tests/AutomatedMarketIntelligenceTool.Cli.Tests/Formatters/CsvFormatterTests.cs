using AutomatedMarketIntelligenceTool.Cli.Formatters;
using FluentAssertions;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Formatters;

public class CsvFormatterTests
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

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        formatter.Format(data);

        // Assert
        var output = stringWriter.ToString();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        lines.Should().HaveCount(3); // Header + 2 data rows
        lines[0].Should().Be("Name,Value"); // Header
        lines[1].Should().Be("Test1,100"); // First row
        lines[2].Should().Be("Test2,200"); // Second row

        // Restore console
        Console.SetOut(originalOut);
    }

    [Fact]
    public void Format_WithEmptyData_ShouldOutputNothing()
    {
        // Arrange
        var formatter = new CsvFormatter();
        var data = Array.Empty<TestObject>();

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        formatter.Format(data);

        // Assert
        var output = stringWriter.ToString();
        output.Should().BeEmpty();

        // Restore console
        Console.SetOut(originalOut);
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

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        formatter.Format(data);

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("\"Test, with comma\"");

        // Restore console
        Console.SetOut(originalOut);
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

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        formatter.Format(data);

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("\"Test \"\"quoted\"\" value\"");

        // Restore console
        Console.SetOut(originalOut);
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

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        formatter.Format(data);

        // Assert
        var output = stringWriter.ToString();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        lines[0].Should().Be("StringProp,IntProp,DecimalProp");
        lines[1].Should().Be("Test,42,99.99");

        // Restore console
        Console.SetOut(originalOut);
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
