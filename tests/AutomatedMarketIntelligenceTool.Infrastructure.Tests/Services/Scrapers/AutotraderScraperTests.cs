using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class AutotraderScraperTests
{
    private readonly AutotraderScraper _scraper;

    public AutotraderScraperTests()
    {
        _scraper = new AutotraderScraper(new NullLogger<AutotraderScraper>());
    }

    [Fact]
    public void SiteName_ShouldReturnAutotrader()
    {
        // Assert
        Assert.Equal("Autotrader.ca", _scraper.SiteName);
    }

    [Fact]
    public void BuildSearchUrl_WithNoParameters_ShouldReturnBaseUrl()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("autotrader.ca/cars", url);
    }

    [Fact]
    public void BuildSearchUrl_WithMake_ShouldIncludeMakeParameter()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            Make = "Toyota"
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("/cars/toyota/", url);
    }

    [Fact]
    public void BuildSearchUrl_WithModel_ShouldIncludeModelParameter()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            Model = "Camry"
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("/cars/camry/", url);
    }

    [Fact]
    public void BuildSearchUrl_WithYearRange_ShouldIncludeYearParameters()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            YearMin = 2018,
            YearMax = 2022
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("ymin=2018", url);
        Assert.Contains("ymax=2022", url);
    }

    [Fact]
    public void BuildSearchUrl_WithPriceRange_ShouldIncludePriceParameters()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            PriceMin = 10000,
            PriceMax = 30000
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("priceMin=10000", url);
        Assert.Contains("priceMax=30000", url);
    }

    [Fact]
    public void BuildSearchUrl_WithMaxMileage_ShouldIncludeMileageParameter()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            MileageMax = 50000
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("odommax=50000", url);
    }

    [Fact]
    public void BuildSearchUrl_WithPostalCode_ShouldIncludeZipParameter()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            PostalCode = "M5V3L9"
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("loc=M5V3L9", url);
    }

    [Fact]
    public void BuildSearchUrl_WithPostalCodeAndRadius_ShouldIncludeBothParameters()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            PostalCode = "M5V3L9",
            RadiusKilometers = 50
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("loc=M5V3L9", url);
        Assert.Contains("radius=50", url);
    }

    [Fact]
    public void BuildSearchUrl_WithPageGreaterThanOne_ShouldIncludeFirstRecordParameter()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 3);

        // Assert
        Assert.Contains("rcp=15&rcs=30", url); // (3-1) * 15 = 30
    }

    [Fact]
    public void BuildSearchUrl_WithPageOne_ShouldNotIncludeFirstRecordParameter()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("rcs=0", url);
    }

    [Fact]
    public void BuildSearchUrl_WithAllParameters_ShouldIncludeAllParameters()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            Make = "Honda",
            Model = "Accord",
            YearMin = 2019,
            YearMax = 2023,
            PriceMin = 15000,
            PriceMax = 35000,
            MileageMax = 60000,
            PostalCode = "M5H2N2",
            RadiusKilometers = 25
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 2);

        // Assert
        Assert.Contains("/cars/honda/accord/", url);
        Assert.Contains("ymin=2019", url);
        Assert.Contains("ymax=2023", url);
        Assert.Contains("priceMin=15000", url);
        Assert.Contains("priceMax=35000", url);
        Assert.Contains("odommax=60000", url);
        Assert.Contains("loc=M5H2N2", url);
        Assert.Contains("radius=25", url);
        Assert.Contains("rcp=15&rcs=15", url);
    }

    private string InvokeBuildSearchUrl(SearchParameters parameters, int page)
    {
        var method = typeof(AutotraderScraper).GetMethod(
            "BuildSearchUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (string)method!.Invoke(_scraper, new object[] { parameters, page })!;
    }
}
