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
        Assert.Equal("Autotrader", _scraper.SiteName);
    }

    [Fact]
    public void BuildSearchUrl_WithNoParameters_ShouldReturnBaseUrl()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("autotrader.com/cars-for-sale/all-cars", url);
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
        Assert.Contains("makeCodeList=TOYOTA", url);
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
        Assert.Contains("modelCodeList=CAMRY", url);
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
        Assert.Contains("startYear=2018", url);
        Assert.Contains("endYear=2022", url);
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
        Assert.Contains("minPrice=10000", url);
        Assert.Contains("maxPrice=30000", url);
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
        Assert.Contains("maxMileage=50000", url);
    }

    [Fact]
    public void BuildSearchUrl_WithZipCode_ShouldIncludeZipParameter()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            ZipCode = "90210"
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("zip=90210", url);
    }

    [Fact]
    public void BuildSearchUrl_WithZipCodeAndRadius_ShouldIncludeBothParameters()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            ZipCode = "90210",
            RadiusMiles = 50
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("zip=90210", url);
        Assert.Contains("searchRadius=50", url);
    }

    [Fact]
    public void BuildSearchUrl_WithPageGreaterThanOne_ShouldIncludeFirstRecordParameter()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 3);

        // Assert
        Assert.Contains("firstRecord=50", url); // (3-1) * 25 = 50
    }

    [Fact]
    public void BuildSearchUrl_WithPageOne_ShouldNotIncludeFirstRecordParameter()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.DoesNotContain("firstRecord", url);
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
            ZipCode = "10001",
            RadiusMiles = 25
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 2);

        // Assert
        Assert.Contains("makeCodeList=HONDA", url);
        Assert.Contains("modelCodeList=ACCORD", url);
        Assert.Contains("startYear=2019", url);
        Assert.Contains("endYear=2023", url);
        Assert.Contains("minPrice=15000", url);
        Assert.Contains("maxPrice=35000", url);
        Assert.Contains("maxMileage=60000", url);
        Assert.Contains("zip=10001", url);
        Assert.Contains("searchRadius=25", url);
        Assert.Contains("firstRecord=25", url);
    }

    private string InvokeBuildSearchUrl(SearchParameters parameters, int page)
    {
        var method = typeof(AutotraderScraper).GetMethod(
            "BuildSearchUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (string)method!.Invoke(_scraper, new object[] { parameters, page })!;
    }
}
