using AutomatedMarketIntelligenceTool.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Commands;

public class ConfigCommandTests
{
    [Fact]
    public void Settings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new ConfigCommand.Settings();

        // Assert
        settings.Operation.Should().BeNull();
        settings.Key.Should().BeNull();
        settings.Value.Should().BeNull();
        settings.ConfigFile.Should().BeNull();
    }

    [Fact]
    public void Settings_CanSetAllProperties()
    {
        // Arrange
        var settings = new ConfigCommand.Settings();

        // Act
        settings.Operation = "set";
        settings.Key = "Database:Provider";
        settings.Value = "SQLServer";
        settings.ConfigFile = "/path/to/config.json";

        // Assert
        settings.Operation.Should().Be("set");
        settings.Key.Should().Be("Database:Provider");
        settings.Value.Should().Be("SQLServer");
        settings.ConfigFile.Should().Be("/path/to/config.json");
    }

    [Fact]
    public void Settings_InheritsFromCommandSettings()
    {
        // Arrange
        var settings = new ConfigCommand.Settings();

        // Assert
        settings.Should().BeAssignableTo<CommandSettings>();
    }

    [Theory]
    [InlineData("list")]
    [InlineData("get")]
    [InlineData("set")]
    [InlineData("reset")]
    [InlineData("path")]
    public void Settings_Operation_AcceptsValidValues(string operation)
    {
        // Arrange & Act
        var settings = new ConfigCommand.Settings
        {
            Operation = operation
        };

        // Assert
        settings.Operation.Should().Be(operation);
    }

    [Theory]
    [InlineData("Database:Provider")]
    [InlineData("Scraping:DefaultDelayMs")]
    [InlineData("Search:DefaultRadius")]
    [InlineData("Output:ColorEnabled")]
    public void Settings_Key_AcceptsValidConfigKeys(string key)
    {
        // Arrange & Act
        var settings = new ConfigCommand.Settings
        {
            Operation = "get",
            Key = key
        };

        // Assert
        settings.Key.Should().Be(key);
    }

    [Theory]
    [InlineData("SQLite")]
    [InlineData("5000")]
    [InlineData("true")]
    [InlineData("false")]
    public void Settings_Value_AcceptsVariousTypes(string value)
    {
        // Arrange & Act
        var settings = new ConfigCommand.Settings
        {
            Operation = "set",
            Key = "SomeKey",
            Value = value
        };

        // Assert
        settings.Value.Should().Be(value);
    }
}
