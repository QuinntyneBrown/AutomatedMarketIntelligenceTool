using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Scrapers;

public class KijijiScraperTests
{
    private readonly KijijiScraper _scraper;

    public KijijiScraperTests()
    {
        _scraper = new KijijiScraper(new NullLogger<KijijiScraper>());
    }

    [Fact]
    public void SiteName_ShouldReturnKijijiDotCa()
    {
        // Assert
        Assert.Equal("Kijiji.ca", _scraper.SiteName);
    }

    [Fact]
    public void BuildSearchUrl_WithNoParameters_ShouldReturnBaseUrl()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("kijiji.ca/b-cars-vehicles", url);
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
        Assert.Contains("carmake=Toyota", url);
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
        Assert.Contains("carmodel=Camry", url);
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
        Assert.Contains("carypmin=2018", url);
        Assert.Contains("carypmax=2022", url);
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
        Assert.Contains("pricemin=10000", url);
        Assert.Contains("pricemax=30000", url);
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
        Assert.Contains("carod=50000", url);
    }

    [Fact]
    public void BuildSearchUrl_WithPostalCode_ShouldIncludePostalCodeInPath()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            PostalCode = "M5V 3L9"
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("m5v3l9", url.ToLower());
    }

    [Fact]
    public void BuildSearchUrl_WithPostalCodeAndRadius_ShouldIncludeRadiusParameter()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            PostalCode = "M5V 3L9",
            RadiusKilometers = 50
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("radius=50", url);
    }

    [Fact]
    public void BuildSearchUrl_WithProvince_ShouldIncludeProvinceInPath()
    {
        // Arrange
        var parameters = new SearchParameters
        {
            Province = CanadianProvince.ON
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.Contains("/on/", url.ToLower());
    }

    [Fact]
    public void BuildSearchUrl_WithPageGreaterThanOne_ShouldIncludePageInPath()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 3);

        // Assert
        Assert.Contains("page-3", url);
    }

    [Fact]
    public void BuildSearchUrl_WithPageOne_ShouldNotIncludePageInPath()
    {
        // Arrange
        var parameters = new SearchParameters();

        // Act
        var url = InvokeBuildSearchUrl(parameters, 1);

        // Assert
        Assert.DoesNotContain("page-", url);
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
            PostalCode = "M5V 3L9",
            RadiusKilometers = 25
        };

        // Act
        var url = InvokeBuildSearchUrl(parameters, 2);

        // Assert
        Assert.Contains("carmake=Honda", url);
        Assert.Contains("carmodel=Accord", url);
        Assert.Contains("carypmin=2019", url);
        Assert.Contains("carypmax=2023", url);
        Assert.Contains("pricemin=15000", url);
        Assert.Contains("pricemax=35000", url);
        Assert.Contains("carod=60000", url);
        Assert.Contains("m5v3l9", url.ToLower());
        Assert.Contains("radius=25", url);
        Assert.Contains("page-2", url);
    }

    private string InvokeBuildSearchUrl(SearchParameters parameters, int page)
    {
        var method = typeof(KijijiScraper).GetMethod(
            "BuildSearchUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (string)method!.Invoke(_scraper, new object[] { parameters, page })!;
    }
}
