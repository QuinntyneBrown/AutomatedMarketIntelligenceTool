using System.Text.Json;
using AutomatedMarketIntelligenceTool.Cli.Formatters;
using FluentAssertions;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Formatters;

public class JsonFormatterTests
{
    [Fact]
    public void Format_WithValidData_ShouldOutputValidJson()
    {
        // Arrange
        var formatter = new JsonFormatter();
        var data = new[]
        {
            new { Name = "Test1", Value = 100 },
            new { Name = "Test2", Value = 200 }
        };

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        formatter.Format(data);

        // Assert
        var output = stringWriter.ToString();
        output.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON
        var deserializedAction = () => JsonSerializer.Deserialize<object[]>(output);
        deserializedAction.Should().NotThrow();

        // Restore console
        Console.SetOut(originalOut);
    }

    [Fact]
    public void Format_WithEmptyData_ShouldOutputEmptyArray()
    {
        // Arrange
        var formatter = new JsonFormatter();
        var data = Array.Empty<TestObject>();

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        formatter.Format(data);

        // Assert
        var output = stringWriter.ToString().Trim();
        output.Should().Be("[]");

        // Restore console
        Console.SetOut(originalOut);
    }

    [Fact]
    public void Format_WithComplexObject_ShouldSerializeAllProperties()
    {
        // Arrange
        var formatter = new JsonFormatter();
        var data = new[]
        {
            new TestObject
            {
                StringProp = "Test",
                IntProp = 42,
                DecimalProp = 99.99m,
                DateProp = new DateTime(2024, 1, 1)
            }
        };

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        formatter.Format(data);

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("stringProp"); // CamelCase naming
        output.Should().Contain("Test");
        output.Should().Contain("42");
        output.Should().Contain("99.99");

        // Restore console
        Console.SetOut(originalOut);
    }

    private class TestObject
    {
        public string StringProp { get; set; } = string.Empty;
        public int IntProp { get; set; }
        public decimal DecimalProp { get; set; }
        public DateTime DateProp { get; set; }
    }
}
