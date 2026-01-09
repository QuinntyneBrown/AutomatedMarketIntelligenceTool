using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Reporting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Reporting;

public class HtmlReportGeneratorTests
{
    private readonly HtmlReportGenerator _generator;
    private readonly string _tempDirectory;

    public HtmlReportGeneratorTests()
    {
        _generator = new HtmlReportGenerator(NullLogger<HtmlReportGenerator>.Instance);
        _tempDirectory = Path.Combine(Path.GetTempPath(), "html-report-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void SupportedFormat_ShouldBeHtml()
    {
        // Assert
        Assert.Equal(ReportFormat.Html, _generator.SupportedFormat);
    }

    [Fact]
    public async Task GenerateReportAsync_WithBasicData_ShouldCreateHtmlFile()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test-report.html");
        var reportData = new ReportData
        {
            Title = "Test Market Report",
            GeneratedAt = DateTime.UtcNow,
            SearchCriteria = "Make: Toyota\nPrice: $20,000 - $30,000",
            Listings = new List<Listing>()
        };

        // Act
        var result = await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));
        
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Test Market Report", content);
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("</html>", content);
    }

    [Fact]
    public async Task GenerateReportAsync_WithListings_ShouldIncludeListingData()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "listings-report.html");
        var tenantId = Guid.NewGuid();
        
        var listing1 = Listing.Create(tenantId, "EXT-001", "autotrader",
            "https://example.com/1", "Toyota", "Camry", 2022, 25000m, Condition.Used);
        listing1.UpdateLocation("Los Angeles, CA", 34.0522m, -118.2437m);
        listing1.UpdateMileage(15000);

        var listing2 = Listing.Create(tenantId, "EXT-002", "cargurus",
            "https://example.com/2", "Honda", "Civic", 2023, 22000m, Condition.New);
        listing2.UpdateLocation("San Francisco, CA", 37.7749m, -122.4194m);
        listing2.UpdateMileage(100);

        var reportData = new ReportData
        {
            Title = "Vehicle Listings Report",
            Listings = new List<Listing> { listing1, listing2 }
        };

        // Act
        var result = await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Toyota", content);
        Assert.Contains("Camry", content);
        Assert.Contains("Honda", content);
        Assert.Contains("Civic", content);
        Assert.Contains("25000", content);
        Assert.Contains("22000", content);
    }

    [Fact]
    public async Task GenerateReportAsync_WithStatistics_ShouldIncludeStats()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "stats-report.html");
        var reportData = new ReportData
        {
            Title = "Market Statistics Report",
            Statistics = new MarketStatistics
            {
                TotalListings = 150,
                AveragePrice = 28500m,
                MedianPrice = 27000m,
                AverageMileage = 25000
            }
        };

        // Act
        await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("150", content);
        Assert.Contains("28500", content);
        Assert.Contains("27000", content);
        Assert.Contains("25000", content);
        Assert.Contains("Market Statistics", content);
    }

    [Fact]
    public async Task GenerateReportAsync_WithSearchCriteria_ShouldIncludeCriteria()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "criteria-report.html");
        var reportData = new ReportData
        {
            Title = "Search Results",
            SearchCriteria = "Make: Toyota\nModel: Camry\nYear: 2020-2024\nPrice: $20,000 - $30,000"
        };

        // Act
        await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Make: Toyota", content);
        Assert.Contains("Model: Camry", content);
        Assert.Contains("Year: 2020-2024", content);
        Assert.Contains("Price: $20,000 - $30,000", content);
    }

    [Fact]
    public async Task GenerateReportAsync_WithEmptyListings_ShouldHandleGracefully()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "empty-report.html");
        var reportData = new ReportData
        {
            Title = "Empty Report",
            Listings = new List<Listing>()
        };

        // Act
        var result = await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        Assert.True(File.Exists(result));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Empty Report", content);
        Assert.Contains("0", content); // Should show 0 listings
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldCreateValidHtmlStructure()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "structure-report.html");
        var reportData = new ReportData { Title = "Structure Test" };

        // Act
        await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("<html", content);
        Assert.Contains("<head>", content);
        Assert.Contains("<body>", content);
        Assert.Contains("</body>", content);
        Assert.Contains("</html>", content);
        Assert.Contains("<style>", content); // Should have CSS
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
