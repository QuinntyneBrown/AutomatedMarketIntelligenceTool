using System.Text.Json;
using AutomatedMarketIntelligenceTool.Cli.Formatters;
using FluentAssertions;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Formatters;

public class JsonFormatterTests : ConsoleTestBase
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

        // Act
        formatter.Format(data);

        // Assert
        var output = GetConsoleOutput();
        output.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON
        var deserializedAction = () => JsonSerializer.Deserialize<object[]>(output);
        deserializedAction.Should().NotThrow();
    }

    [Fact]
    public void Format_WithEmptyData_ShouldOutputEmptyArray()
    {
        // Arrange
        var formatter = new JsonFormatter();
        var data = Array.Empty<TestObject>();

        // Act
        formatter.Format(data);

        // Assert
        var output = GetConsoleOutput();
        output.Should().Contain("[]");
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

        // Act
        formatter.Format(data);

        // Assert
        var output = GetConsoleOutput();
        output.Should().Contain("stringProp"); // CamelCase naming
        output.Should().Contain("Test");
        output.Should().Contain("42");
        output.Should().Contain("99.99");
    }

    private class TestObject
    {
        public string StringProp { get; set; } = string.Empty;
        public int IntProp { get; set; }
        public decimal DecimalProp { get; set; }
        public DateTime DateProp { get; set; }
    }
}
