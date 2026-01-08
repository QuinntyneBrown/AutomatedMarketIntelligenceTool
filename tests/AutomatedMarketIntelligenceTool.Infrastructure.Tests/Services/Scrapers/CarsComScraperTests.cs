using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class CarsComScraperTests
{
    private readonly CarsComScraper _scraper;

    public CarsComScraperTests()
    {
        _scraper = new CarsComScraper(new NullLogger<CarsComScraper>());
    }

    [Fact]
    public void SiteName_ShouldReturnCarsDotCom()
    {
        // Assert
        Assert.Equal("Cars.com", _scraper.SiteName);
    }

    [Fact]
    public void BuildSearchUrl_WithNoParameters_ShouldReturnBaseUrl()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("cars.com/shopping/results", url);
    }

    [Fact]
    public void BuildSearchUrl_WithMake_ShouldIncludeMakesParameter()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            Make = "Toyota"
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("makes[]=toyota", url);
    }

    [Fact]
    public void BuildSearchUrl_WithModel_ShouldIncludeModelsParameter()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            Model = "Camry"
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("models[]=camry", url);
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
        Assert.Contains("year_min=2018", url);
        Assert.Contains("year_max=2022", url);
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
        Assert.Contains("price_min=10000", url);
        Assert.Contains("price_max=30000", url);
    }

    [Fact]
    public void BuildSearchUrl_WithMaxMileage_ShouldIncludeMaximumDistanceParameter()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            MileageMax = 50000
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("maximum_distance=50000", url);
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
        Assert.Contains("radius=50", url);
    }

    [Fact]
    public void BuildSearchUrl_WithPageGreaterThanOne_ShouldIncludePageParameter()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 3);

        // Assert
        Assert.Contains("page=3", url);
    }

    [Fact]
    public void BuildSearchUrl_WithPageOne_ShouldNotIncludePageParameter()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.DoesNotContain("page=", url);
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
        Assert.Contains("makes[]=honda", url);
        Assert.Contains("models[]=accord", url);
        Assert.Contains("year_min=2019", url);
        Assert.Contains("year_max=2023", url);
        Assert.Contains("price_min=15000", url);
        Assert.Contains("price_max=35000", url);
        Assert.Contains("maximum_distance=60000", url);
        Assert.Contains("zip=10001", url);
        Assert.Contains("radius=25", url);
        Assert.Contains("page=2", url);
    }

    private string InvokeBuildSearchUrl(SearchParameters parameters, int page)
    {
        var method = typeof(CarsComScraper).GetMethod(
            "BuildSearchUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (string)method!.Invoke(_scraper, new object[] { parameters, page })!;
    }
}
