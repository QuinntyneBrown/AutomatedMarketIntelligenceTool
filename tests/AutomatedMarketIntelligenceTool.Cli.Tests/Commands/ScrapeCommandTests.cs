using AutomatedMarketIntelligenceTool.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Commands;

public class ScrapeCommandTests
{
    [Fact]
    public void Settings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new ScrapeCommand.Settings();

        // Assert
        settings.Site.Should().Be("all");
        settings.MaxPages.Should().Be(50);
    }

    [Fact]
    public void Settings_CanSetAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var settings = new ScrapeCommand.Settings();

        // Act
        settings.TenantId = tenantId;
        settings.Site = "autotrader";
        settings.Make = "Toyota";
        settings.Model = "Camry";
        settings.YearMin = 2020;
        settings.YearMax = 2024;
        settings.PriceMin = 10000;
        settings.PriceMax = 50000;
        settings.MileageMax = 50000;
        settings.ZipCode = "90210";
        settings.Radius = 50;
        settings.MaxPages = 10;

        // Assert
        settings.TenantId.Should().Be(tenantId);
        settings.Site.Should().Be("autotrader");
        settings.Make.Should().Be("Toyota");
        settings.Model.Should().Be("Camry");
        settings.YearMin.Should().Be(2020);
        settings.YearMax.Should().Be(2024);
        settings.PriceMin.Should().Be(10000);
        settings.PriceMax.Should().Be(50000);
        settings.MileageMax.Should().Be(50000);
        settings.ZipCode.Should().Be("90210");
        settings.Radius.Should().Be(50);
        settings.MaxPages.Should().Be(10);
    }

    [Fact]
    public void Settings_OptionalPropertiesCanBeNull()
    {
        // Arrange & Act
        var settings = new ScrapeCommand.Settings
        {
            TenantId = Guid.NewGuid()
        };

        // Assert
        settings.Make.Should().BeNull();
        settings.Model.Should().BeNull();
        settings.YearMin.Should().BeNull();
        settings.YearMax.Should().BeNull();
        settings.PriceMin.Should().BeNull();
        settings.PriceMax.Should().BeNull();
        settings.MileageMax.Should().BeNull();
        settings.ZipCode.Should().BeNull();
        settings.Radius.Should().BeNull();
    }

    [Fact]
    public void Settings_InheritsFromCommandSettings()
    {
        // Arrange
        var settings = new ScrapeCommand.Settings();

        // Assert
        settings.Should().BeAssignableTo<CommandSettings>();
    }
}
