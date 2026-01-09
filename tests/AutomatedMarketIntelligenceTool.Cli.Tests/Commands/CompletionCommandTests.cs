using AutomatedMarketIntelligenceTool.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Commands;

public class CompletionCommandTests
{
    [Fact]
    public void Settings_DefaultValues_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new CompletionCommand.Settings();

        // Assert
        settings.Shell.Should().BeNull();
        settings.OutputPath.Should().BeNull();
    }

    [Fact]
    public void Settings_CanSetShell()
    {
        // Arrange
        var settings = new CompletionCommand.Settings();

        // Act
        settings.Shell = "bash";

        // Assert
        settings.Shell.Should().Be("bash");
    }

    [Fact]
    public void Settings_CanSetOutputPath()
    {
        // Arrange
        var settings = new CompletionCommand.Settings();

        // Act
        settings.OutputPath = "/path/to/completions.sh";

        // Assert
        settings.OutputPath.Should().Be("/path/to/completions.sh");
    }

    [Fact]
    public void Settings_InheritsFromCommandSettings()
    {
        // Arrange
        var settings = new CompletionCommand.Settings();

        // Assert
        settings.Should().BeAssignableTo<CommandSettings>();
    }

    [Theory]
    [InlineData("bash")]
    [InlineData("zsh")]
    [InlineData("powershell")]
    [InlineData("pwsh")]
    [InlineData("fish")]
    public void Settings_Shell_AcceptsValidShellTypes(string shell)
    {
        // Arrange & Act
        var settings = new CompletionCommand.Settings
        {
            Shell = shell
        };

        // Assert
        settings.Shell.Should().Be(shell);
    }

    [Fact]
    public void CompletionCommand_CanBeInstantiated()
    {
        // Arrange & Act
        var command = new CompletionCommand();

        // Assert
        command.Should().NotBeNull();
    }

    [Fact]
    public void Settings_CanSetAllProperties()
    {
        // Arrange & Act
        var settings = new CompletionCommand.Settings
        {
            Shell = "zsh",
            OutputPath = "~/.zsh/completions/_car-search"
        };

        // Assert
        settings.Shell.Should().Be("zsh");
        settings.OutputPath.Should().Be("~/.zsh/completions/_car-search");
    }
}
