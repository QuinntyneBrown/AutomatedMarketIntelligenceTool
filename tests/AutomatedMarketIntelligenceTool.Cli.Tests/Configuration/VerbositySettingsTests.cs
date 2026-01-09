using AutomatedMarketIntelligenceTool.Cli.Configuration;
using FluentAssertions;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Configuration;

public class VerbositySettingsTests
{
    [Fact]
    public void VerbositySettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new VerbositySettings();

        // Assert
        settings.DefaultLevel.Should().Be(0);
        settings.EnableFileLogging.Should().BeTrue();
        settings.LogDirectory.Should().Be("logs");
        settings.RetainDays.Should().Be(7);
    }

    [Fact]
    public void VerbositySettings_CanModifyAllProperties()
    {
        // Arrange
        var settings = new VerbositySettings();

        // Act
        settings.DefaultLevel = 2;
        settings.EnableFileLogging = false;
        settings.LogDirectory = "/var/log/app";
        settings.RetainDays = 30;

        // Assert
        settings.DefaultLevel.Should().Be(2);
        settings.EnableFileLogging.Should().BeFalse();
        settings.LogDirectory.Should().Be("/var/log/app");
        settings.RetainDays.Should().Be(30);
    }
}

public class InteractiveSettingsTests
{
    [Fact]
    public void InteractiveSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new InteractiveSettings();

        // Assert
        settings.RememberPreviousValues.Should().BeTrue();
        settings.DefaultTenantId.Should().BeNull();
        settings.ShowCommandPreview.Should().BeTrue();
    }

    [Fact]
    public void InteractiveSettings_CanModifyAllProperties()
    {
        // Arrange
        var settings = new InteractiveSettings();
        var tenantId = Guid.NewGuid();

        // Act
        settings.RememberPreviousValues = false;
        settings.DefaultTenantId = tenantId;
        settings.ShowCommandPreview = false;

        // Assert
        settings.RememberPreviousValues.Should().BeFalse();
        settings.DefaultTenantId.Should().Be(tenantId);
        settings.ShowCommandPreview.Should().BeFalse();
    }
}

public class AppSettingsWithVerbosityTests
{
    [Fact]
    public void AppSettings_IncludesVerbositySettings()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        settings.Verbosity.Should().NotBeNull();
        settings.Verbosity.Should().BeOfType<VerbositySettings>();
    }

    [Fact]
    public void AppSettings_IncludesInteractiveSettings()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        settings.Interactive.Should().NotBeNull();
        settings.Interactive.Should().BeOfType<InteractiveSettings>();
    }

    [Fact]
    public void AppSettings_VerbosityAndInteractive_HaveDefaults()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert - Verbosity defaults
        settings.Verbosity.DefaultLevel.Should().Be(0);
        settings.Verbosity.EnableFileLogging.Should().BeTrue();

        // Assert - Interactive defaults
        settings.Interactive.RememberPreviousValues.Should().BeTrue();
        settings.Interactive.ShowCommandPreview.Should().BeTrue();
    }
}
