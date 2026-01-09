using AutomatedMarketIntelligenceTool.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Commands;

public class SearchCommandTests
{
    [Fact]
    public void Settings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new SearchCommand.Settings();

        // Assert
        settings.Radius.Should().Be(40);
        settings.Format.Should().Be("table");
        settings.Page.Should().Be(1);
        settings.PageSize.Should().Be(30);
        settings.Interactive.Should().BeFalse();
    }

    [Fact]
    public void Settings_CanSetAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var settings = new SearchCommand.Settings();

        // Act
        settings.TenantId = tenantId;
        settings.Makes = new[] { "Toyota", "Honda" };
        settings.Models = new[] { "Camry", "Civic" };
        settings.YearMin = 2020;
        settings.YearMax = 2024;
        settings.PriceMin = 10000;
        settings.PriceMax = 50000;
        settings.MileageMin = 0;
        settings.MileageMax = 50000;
        settings.ZipCode = "M5V 3L9";
        settings.Radius = 50;
        settings.Format = "json";
        settings.Page = 2;
        settings.PageSize = 50;

        // Assert
        settings.TenantId.Should().Be(tenantId);
        settings.Makes.Should().BeEquivalentTo(new[] { "Toyota", "Honda" });
        settings.Models.Should().BeEquivalentTo(new[] { "Camry", "Civic" });
        settings.YearMin.Should().Be(2020);
        settings.YearMax.Should().Be(2024);
        settings.PriceMin.Should().Be(10000);
        settings.PriceMax.Should().Be(50000);
        settings.MileageMin.Should().Be(0);
        settings.MileageMax.Should().Be(50000);
        settings.ZipCode.Should().Be("M5V 3L9");
        settings.Radius.Should().Be(50);
        settings.Format.Should().Be("json");
        settings.Page.Should().Be(2);
        settings.PageSize.Should().Be(50);
    }

    [Fact]
    public void Settings_OptionalPropertiesCanBeNull()
    {
        // Arrange & Act
        var settings = new SearchCommand.Settings
        {
            TenantId = Guid.NewGuid()
        };

        // Assert
        settings.Makes.Should().BeNull();
        settings.Models.Should().BeNull();
        settings.YearMin.Should().BeNull();
        settings.YearMax.Should().BeNull();
        settings.PriceMin.Should().BeNull();
        settings.PriceMax.Should().BeNull();
        settings.MileageMin.Should().BeNull();
        settings.MileageMax.Should().BeNull();
        settings.ZipCode.Should().BeNull();
    }

    [Fact]
    public void Settings_InheritsFromCommandSettings()
    {
        // Arrange
        var settings = new SearchCommand.Settings();

        // Assert
        settings.Should().BeAssignableTo<CommandSettings>();
    }

    [Fact]
    public void Settings_Interactive_CanBeEnabled()
    {
        // Arrange
        var settings = new SearchCommand.Settings();

        // Act
        settings.Interactive = true;

        // Assert
        settings.Interactive.Should().BeTrue();
    }

    [Fact]
    public void Settings_Interactive_DefaultsToFalse()
    {
        // Arrange & Act
        var settings = new SearchCommand.Settings();

        // Assert
        settings.Interactive.Should().BeFalse();
    }

    [Fact]
    public void Settings_Interactive_CanBeSetWithAllOtherProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var settings = new SearchCommand.Settings();

        // Act
        settings.TenantId = tenantId;
        settings.Interactive = true;
        settings.Makes = new[] { "Toyota" };
        settings.Format = "json";

        // Assert
        settings.TenantId.Should().Be(tenantId);
        settings.Interactive.Should().BeTrue();
        settings.Makes.Should().BeEquivalentTo(new[] { "Toyota" });
        settings.Format.Should().Be("json");
    }
}
