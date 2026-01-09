using AutomatedMarketIntelligenceTool.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Commands;

public class RestoreCommandTests
{
    [Fact]
    public void Settings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new RestoreCommand.Settings();

        // Assert
        settings.BackupFilePath.Should().BeEmpty();
        settings.Yes.Should().BeFalse();
    }

    [Fact]
    public void Settings_CanSetAllProperties()
    {
        // Arrange
        var settings = new RestoreCommand.Settings();

        // Act
        settings.BackupFilePath = "/path/to/backup.db";
        settings.Yes = true;

        // Assert
        settings.BackupFilePath.Should().Be("/path/to/backup.db");
        settings.Yes.Should().BeTrue();
    }

    [Fact]
    public void Settings_InheritsFromCommandSettings()
    {
        // Arrange
        var settings = new RestoreCommand.Settings();

        // Assert
        settings.Should().BeAssignableTo<CommandSettings>();
    }

    [Fact]
    public void Settings_Yes_CanBeSetForSkippingConfirmation()
    {
        // Arrange & Act
        var settings = new RestoreCommand.Settings
        {
            BackupFilePath = "/path/to/backup.db",
            Yes = true
        };

        // Assert
        settings.Yes.Should().BeTrue();
    }

    [Theory]
    [InlineData("/var/backups/car-search.db")]
    [InlineData("./backups/backup-2024-01-09.db")]
    [InlineData("C:\\backups\\car-search.db")]
    [InlineData("backup.db")]
    public void Settings_BackupFilePath_AcceptsVariousPaths(string path)
    {
        // Arrange & Act
        var settings = new RestoreCommand.Settings
        {
            BackupFilePath = path
        };

        // Assert
        settings.BackupFilePath.Should().Be(path);
    }

    [Fact]
    public void Settings_BackupFilePath_IsRequiredArgument()
    {
        // This test verifies the attribute configuration
        // Arrange
        var propertyInfo = typeof(RestoreCommand.Settings).GetProperty("BackupFilePath");

        // Assert
        propertyInfo.Should().NotBeNull();
        var attribute = propertyInfo!.GetCustomAttributes(typeof(CommandArgumentAttribute), false).FirstOrDefault();
        attribute.Should().NotBeNull();
    }
}
