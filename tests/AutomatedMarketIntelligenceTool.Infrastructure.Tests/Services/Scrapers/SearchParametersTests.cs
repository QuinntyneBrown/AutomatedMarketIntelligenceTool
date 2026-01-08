using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class SearchParametersTests
{
    [Fact]
    public void SearchParameters_CanBeInstantiated()
    {
        // Act
        var parameters = new SearchParameters();

        // Assert
        Assert.NotNull(parameters);
    }

    [Fact]
    public void SearchParameters_AllPropertiesAreNullableOrOptional()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Assert
        Assert.Null(parameters.Make);
        Assert.Null(parameters.Model);
        Assert.Null(parameters.YearMin);
        Assert.Null(parameters.YearMax);
        Assert.Null(parameters.PriceMin);
        Assert.Null(parameters.PriceMax);
        Assert.Null(parameters.MileageMax);
        Assert.Null(parameters.PostalCode);
        Assert.Null(parameters.RadiusKilometers);
        Assert.Null(parameters.Province);
        Assert.Null(parameters.MaxPages);
    }

    [Fact]
    public void SearchParameters_CanSetAllProperties()
    {
        // Arrange & Act
        var parameters = new SearchParameters
        {
            Make = "Toyota",
            Model = "Camry",
            YearMin = 2018,
            YearMax = 2022,
            PriceMin = 15000,
            PriceMax = 30000,
            MileageMax = 50000,
            PostalCode = "M5V 3L9",
            RadiusKilometers = 25,
            Province = CanadianProvince.ON,
            MaxPages = 5
        };

        // Assert
        Assert.Equal("Toyota", parameters.Make);
        Assert.Equal("Camry", parameters.Model);
        Assert.Equal(2018, parameters.YearMin);
        Assert.Equal(2022, parameters.YearMax);
        Assert.Equal(15000, parameters.PriceMin);
        Assert.Equal(30000, parameters.PriceMax);
        Assert.Equal(50000, parameters.MileageMax);
        Assert.Equal("M5V 3L9", parameters.PostalCode);
        Assert.Equal(25, parameters.RadiusKilometers);
        Assert.Equal(CanadianProvince.ON, parameters.Province);
        Assert.Equal(5, parameters.MaxPages);
    }
}
