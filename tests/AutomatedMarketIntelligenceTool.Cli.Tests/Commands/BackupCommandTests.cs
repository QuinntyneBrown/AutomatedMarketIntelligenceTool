using AutomatedMarketIntelligenceTool.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Commands;

public class BackupCommandTests
{
    [Fact]
    public void Settings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new BackupCommand.Settings();

        // Assert
        settings.OutputPath.Should().BeNull();
        settings.List.Should().BeFalse();
        settings.Cleanup.Should().BeFalse();
        settings.RetentionCount.Should().Be(5);
        settings.Yes.Should().BeFalse();
    }

    [Fact]
    public void Settings_CanSetAllProperties()
    {
        // Arrange
        var settings = new BackupCommand.Settings();

        // Act
        settings.OutputPath = "/path/to/backup.db";
        settings.List = true;
        settings.Cleanup = true;
        settings.RetentionCount = 10;
        settings.Yes = true;

        // Assert
        settings.OutputPath.Should().Be("/path/to/backup.db");
        settings.List.Should().BeTrue();
        settings.Cleanup.Should().BeTrue();
        settings.RetentionCount.Should().Be(10);
        settings.Yes.Should().BeTrue();
    }

    [Fact]
    public void Settings_InheritsFromCommandSettings()
    {
        // Arrange
        var settings = new BackupCommand.Settings();

        // Assert
        settings.Should().BeAssignableTo<CommandSettings>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public void Settings_RetentionCount_AcceptsValidValues(int retention)
    {
        // Arrange & Act
        var settings = new BackupCommand.Settings
        {
            RetentionCount = retention
        };

        // Assert
        settings.RetentionCount.Should().Be(retention);
    }

    [Fact]
    public void Settings_Yes_CanBeSetForSkippingConfirmation()
    {
        // Arrange & Act
        var settings = new BackupCommand.Settings
        {
            Cleanup = true,
            Yes = true
        };

        // Assert
        settings.Yes.Should().BeTrue();
        settings.Cleanup.Should().BeTrue();
    }

    [Fact]
    public void Settings_OutputPath_AcceptsAbsolutePath()
    {
        // Arrange & Act
        var settings = new BackupCommand.Settings
        {
            OutputPath = "/var/backups/car-search/backup-2024.db"
        };

        // Assert
        settings.OutputPath.Should().Be("/var/backups/car-search/backup-2024.db");
    }

    [Fact]
    public void Settings_OutputPath_AcceptsRelativePath()
    {
        // Arrange & Act
        var settings = new BackupCommand.Settings
        {
            OutputPath = "./backups/backup.db"
        };

        // Assert
        settings.OutputPath.Should().Be("./backups/backup.db");
    }
}
