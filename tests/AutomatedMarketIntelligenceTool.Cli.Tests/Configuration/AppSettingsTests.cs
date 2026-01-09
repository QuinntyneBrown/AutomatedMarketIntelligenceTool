using AutomatedMarketIntelligenceTool.Cli.Configuration;
using FluentAssertions;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Configuration;

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        settings.Database.Should().NotBeNull();
        settings.Scraping.Should().NotBeNull();
        settings.Search.Should().NotBeNull();
        settings.Deactivation.Should().NotBeNull();
        settings.Output.Should().NotBeNull();
        settings.Verbosity.Should().NotBeNull();
        settings.Interactive.Should().NotBeNull();
    }

    [Fact]
    public void DatabaseSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new DatabaseSettings();

        // Assert
        settings.Provider.Should().Be("SQLite");
        settings.SQLitePath.Should().Be("car-search.db");
        settings.ConnectionString.Should().BeNull();
    }

    [Fact]
    public void ScrapingSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new ScrapingSettings();

        // Assert
        settings.DefaultDelayMs.Should().Be(3000);
        settings.MaxRetries.Should().Be(3);
        settings.MaxPages.Should().Be(50);
        settings.DefaultSites.Should().BeEquivalentTo(new[] { "autotrader", "cars.com", "cargurus" });
        settings.HeadedMode.Should().BeFalse();
    }

    [Fact]
    public void SearchSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new SearchSettings();

        // Assert
        settings.DefaultRadius.Should().Be(40);
        settings.DefaultPageSize.Should().Be(25);
    }

    [Fact]
    public void DeactivationSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new DeactivationSettings();

        // Assert
        settings.StaleDays.Should().Be(7);
    }

    [Fact]
    public void OutputSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new OutputSettings();

        // Assert
        settings.DefaultFormat.Should().Be("table");
        settings.ColorEnabled.Should().BeTrue();
    }

    [Fact]
    public void AppSettings_CanModifyAllSettings()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.Database.Provider = "SQLServer";
        settings.Scraping.DefaultDelayMs = 5000;
        settings.Search.DefaultRadius = 100;
        settings.Deactivation.StaleDays = 14;
        settings.Output.DefaultFormat = "json";

        // Assert
        settings.Database.Provider.Should().Be("SQLServer");
        settings.Scraping.DefaultDelayMs.Should().Be(5000);
        settings.Search.DefaultRadius.Should().Be(100);
        settings.Deactivation.StaleDays.Should().Be(14);
        settings.Output.DefaultFormat.Should().Be("json");
    }
}
