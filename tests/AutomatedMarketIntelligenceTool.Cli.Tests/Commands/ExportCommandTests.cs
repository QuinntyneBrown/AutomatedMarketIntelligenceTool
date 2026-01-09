using AutomatedMarketIntelligenceTool.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Commands;

public class ExportCommandTests
{
    [Fact]
    public void Settings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new ExportCommand.Settings();

        // Assert
        settings.Radius.Should().Be(40);
        settings.SortDescending.Should().BeFalse();
    }

    [Fact]
    public void Settings_CanSetAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var settings = new ExportCommand.Settings();

        // Act
        settings.OutputFile = "output.json";
        settings.Format = "json";
        settings.TenantId = tenantId;
        settings.Makes = new[] { "Toyota", "Honda" };
        settings.Models = new[] { "Camry", "Civic" };
        settings.YearMin = 2020;
        settings.YearMax = 2024;
        settings.PriceMin = 10000;
        settings.PriceMax = 50000;
        settings.MileageMin = 0;
        settings.MileageMax = 50000;
        settings.Conditions = new[] { "Used" };
        settings.Transmissions = new[] { "Automatic" };
        settings.FuelTypes = new[] { "Gasoline" };
        settings.BodyStyles = new[] { "Sedan" };
        settings.City = "Toronto";
        settings.State = "ON";
        settings.PostalCode = "M5V 3L9";
        settings.Radius = 50;
        settings.SortBy = "Price";
        settings.SortDescending = true;

        // Assert
        settings.OutputFile.Should().Be("output.json");
        settings.Format.Should().Be("json");
        settings.TenantId.Should().Be(tenantId);
        settings.Makes.Should().BeEquivalentTo(new[] { "Toyota", "Honda" });
        settings.Models.Should().BeEquivalentTo(new[] { "Camry", "Civic" });
        settings.YearMin.Should().Be(2020);
        settings.YearMax.Should().Be(2024);
        settings.PriceMin.Should().Be(10000);
        settings.PriceMax.Should().Be(50000);
        settings.MileageMin.Should().Be(0);
        settings.MileageMax.Should().Be(50000);
        settings.Conditions.Should().BeEquivalentTo(new[] { "Used" });
        settings.Transmissions.Should().BeEquivalentTo(new[] { "Automatic" });
        settings.FuelTypes.Should().BeEquivalentTo(new[] { "Gasoline" });
        settings.BodyStyles.Should().BeEquivalentTo(new[] { "Sedan" });
        settings.City.Should().Be("Toronto");
        settings.State.Should().Be("ON");
        settings.PostalCode.Should().Be("M5V 3L9");
        settings.Radius.Should().Be(50);
        settings.SortBy.Should().Be("Price");
        settings.SortDescending.Should().BeTrue();
    }

    [Fact]
    public void Settings_OptionalPropertiesCanBeNull()
    {
        // Arrange & Act
        var settings = new ExportCommand.Settings
        {
            TenantId = Guid.NewGuid(),
            OutputFile = "output.csv"
        };

        // Assert
        settings.Format.Should().BeNull();
        settings.Makes.Should().BeNull();
        settings.Models.Should().BeNull();
        settings.YearMin.Should().BeNull();
        settings.YearMax.Should().BeNull();
        settings.PriceMin.Should().BeNull();
        settings.PriceMax.Should().BeNull();
        settings.MileageMin.Should().BeNull();
        settings.MileageMax.Should().BeNull();
        settings.Conditions.Should().BeNull();
        settings.Transmissions.Should().BeNull();
        settings.FuelTypes.Should().BeNull();
        settings.BodyStyles.Should().BeNull();
        settings.City.Should().BeNull();
        settings.State.Should().BeNull();
        settings.PostalCode.Should().BeNull();
        settings.SortBy.Should().BeNull();
    }

    [Fact]
    public void Settings_InheritsFromCommandSettings()
    {
        // Arrange
        var settings = new ExportCommand.Settings();

        // Assert
        settings.Should().BeAssignableTo<CommandSettings>();
    }

    [Theory]
    [InlineData("results.json")]
    [InlineData("results.csv")]
    [InlineData("/tmp/export.json")]
    [InlineData("C:\\data\\output.csv")]
    public void Settings_OutputFile_AcceptsValidPaths(string outputFile)
    {
        // Arrange & Act
        var settings = new ExportCommand.Settings
        {
            TenantId = Guid.NewGuid(),
            OutputFile = outputFile
        };

        // Assert
        settings.OutputFile.Should().Be(outputFile);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("csv")]
    public void Settings_Format_AcceptsValidValues(string format)
    {
        // Arrange & Act
        var settings = new ExportCommand.Settings
        {
            TenantId = Guid.NewGuid(),
            OutputFile = "output.txt",
            Format = format
        };

        // Assert
        settings.Format.Should().Be(format);
    }
}
