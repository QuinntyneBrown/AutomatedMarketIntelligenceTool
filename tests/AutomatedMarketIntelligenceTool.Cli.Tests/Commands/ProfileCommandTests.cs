using AutomatedMarketIntelligenceTool.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Commands;

public class ProfileCommandTests
{
    [Fact]
    public void Settings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new ProfileCommand.Settings();

        // Assert
        settings.Action.Should().BeEmpty();
        settings.ProfileName.Should().BeNull();
        settings.TenantId.Should().Be(Guid.Empty);
        settings.Description.Should().BeNull();
        settings.Yes.Should().BeFalse();
        settings.Make.Should().BeNull();
        settings.Model.Should().BeNull();
    }

    [Fact]
    public void Settings_CanSetAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var settings = new ProfileCommand.Settings();

        // Act
        settings.Action = "save";
        settings.ProfileName = "my-profile";
        settings.TenantId = tenantId;
        settings.Description = "My test profile";
        settings.Yes = true;
        settings.Make = "Toyota,Honda";
        settings.Model = "Camry,Accord";
        settings.YearMin = 2020;
        settings.YearMax = 2024;
        settings.PriceMin = 20000m;
        settings.PriceMax = 50000m;
        settings.MileageMax = 100000;
        settings.Condition = "Used";
        settings.BodyStyle = "sedan";
        settings.Drivetrain = "AWD";

        // Assert
        settings.Action.Should().Be("save");
        settings.ProfileName.Should().Be("my-profile");
        settings.TenantId.Should().Be(tenantId);
        settings.Description.Should().Be("My test profile");
        settings.Yes.Should().BeTrue();
        settings.Make.Should().Be("Toyota,Honda");
        settings.Model.Should().Be("Camry,Accord");
        settings.YearMin.Should().Be(2020);
        settings.YearMax.Should().Be(2024);
        settings.PriceMin.Should().Be(20000m);
        settings.PriceMax.Should().Be(50000m);
        settings.MileageMax.Should().Be(100000);
        settings.Condition.Should().Be("Used");
        settings.BodyStyle.Should().Be("sedan");
        settings.Drivetrain.Should().Be("AWD");
    }

    [Fact]
    public void Settings_InheritsFromCommandSettings()
    {
        // Arrange
        var settings = new ProfileCommand.Settings();

        // Assert
        settings.Should().BeAssignableTo<CommandSettings>();
    }

    [Theory]
    [InlineData("save")]
    [InlineData("load")]
    [InlineData("list")]
    [InlineData("delete")]
    public void Settings_Action_AcceptsValidValues(string action)
    {
        // Arrange & Act
        var settings = new ProfileCommand.Settings
        {
            Action = action
        };

        // Assert
        settings.Action.Should().Be(action);
    }

    [Fact]
    public void Settings_Yes_CanBeSetForSkippingDeleteConfirmation()
    {
        // Arrange & Act
        var settings = new ProfileCommand.Settings
        {
            Action = "delete",
            ProfileName = "profile-to-delete",
            Yes = true
        };

        // Assert
        settings.Action.Should().Be("delete");
        settings.Yes.Should().BeTrue();
    }

    [Fact]
    public void Settings_TenantId_AcceptsValidGuid()
    {
        // Arrange
        var tenantId = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var settings = new ProfileCommand.Settings
        {
            TenantId = tenantId
        };

        // Assert
        settings.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Settings_SearchParameters_CanBeSetForSaveAction()
    {
        // Arrange & Act
        var settings = new ProfileCommand.Settings
        {
            Action = "save",
            ProfileName = "family-suv",
            Make = "Toyota,Honda,Mazda",
            BodyStyle = "suv",
            YearMin = 2021,
            PriceMax = 45000m,
            Drivetrain = "AWD"
        };

        // Assert
        settings.Make.Should().Be("Toyota,Honda,Mazda");
        settings.BodyStyle.Should().Be("suv");
        settings.YearMin.Should().Be(2021);
        settings.PriceMax.Should().Be(45000m);
        settings.Drivetrain.Should().Be("AWD");
    }

    [Theory]
    [InlineData("New")]
    [InlineData("Used")]
    [InlineData("Certified")]
    public void Settings_Condition_AcceptsValidValues(string condition)
    {
        // Arrange & Act
        var settings = new ProfileCommand.Settings
        {
            Condition = condition
        };

        // Assert
        settings.Condition.Should().Be(condition);
    }

    [Theory]
    [InlineData("sedan")]
    [InlineData("suv")]
    [InlineData("truck")]
    [InlineData("coupe")]
    [InlineData("hatchback")]
    public void Settings_BodyStyle_AcceptsValidValues(string bodyStyle)
    {
        // Arrange & Act
        var settings = new ProfileCommand.Settings
        {
            BodyStyle = bodyStyle
        };

        // Assert
        settings.BodyStyle.Should().Be(bodyStyle);
    }

    [Theory]
    [InlineData("FWD")]
    [InlineData("RWD")]
    [InlineData("AWD")]
    [InlineData("4WD")]
    public void Settings_Drivetrain_AcceptsValidValues(string drivetrain)
    {
        // Arrange & Act
        var settings = new ProfileCommand.Settings
        {
            Drivetrain = drivetrain
        };

        // Assert
        settings.Drivetrain.Should().Be(drivetrain);
    }
}
