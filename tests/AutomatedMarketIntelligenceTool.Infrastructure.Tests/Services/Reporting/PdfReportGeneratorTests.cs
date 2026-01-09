using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Reporting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Reporting;

public class PdfReportGeneratorTests
{
    private readonly PdfReportGenerator _generator;
    private readonly string _tempDirectory;

    public PdfReportGeneratorTests()
    {
        _generator = new PdfReportGenerator(NullLogger<PdfReportGenerator>.Instance);
        _tempDirectory = Path.Combine(Path.GetTempPath(), "pdf-report-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void SupportedFormat_ShouldBePdf()
    {
        // Assert
        Assert.Equal(ReportFormat.Pdf, _generator.SupportedFormat);
    }

    [Fact]
    public async Task GenerateReportAsync_WithBasicData_ShouldCreatePdfFile()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test-report.pdf");
        var reportData = new ReportData
        {
            Title = "Test PDF Report",
            GeneratedAt = DateTime.UtcNow,
            Listings = new List<Listing>()
        };

        // Act
        var result = await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));
        
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 0);
    }

    [Fact]
    public async Task GenerateReportAsync_WithListings_ShouldCreatePdfWithData()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "listings-report.pdf");
        var tenantId = Guid.NewGuid();
        
        var listing = Listing.Create(tenantId, "EXT-001", "autotrader",
            "https://example.com/1", "Toyota", "Camry", 2022, 25000m, Condition.Used);
        listing.UpdateLocation("Los Angeles, CA", 34.0522m, -118.2437m);
        listing.UpdateMileage(15000);

        var reportData = new ReportData
        {
            Title = "Vehicle Listings PDF",
            Listings = new List<Listing> { listing }
        };

        // Act
        var result = await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        Assert.True(File.Exists(result));
        var fileInfo = new FileInfo(result);
        Assert.True(fileInfo.Length > 0);
    }

    [Fact]
    public async Task GenerateReportAsync_WithStatistics_ShouldIncludeStats()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "stats-report.pdf");
        var reportData = new ReportData
        {
            Title = "Statistics Report",
            Statistics = new MarketStatistics
            {
                TotalListings = 150,
                AveragePrice = 28500m,
                MedianPrice = 27000m,
                AverageMileage = 25000
            }
        };

        // Act
        var result = await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        Assert.True(File.Exists(result));
        var fileInfo = new FileInfo(result);
        Assert.True(fileInfo.Length > 0);
    }

    [Fact]
    public async Task GenerateReportAsync_WithManyListings_ShouldHandleLargeDataset()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "large-report.pdf");
        var tenantId = Guid.NewGuid();
        var listings = new List<Listing>();

        for (int i = 0; i < 150; i++)
        {
            var listing = Listing.Create(tenantId, $"EXT-{i:D3}", "autotrader",
                $"https://example.com/{i}", "Toyota", "Camry", 2020 + (i % 5), 20000m + (i * 100), Condition.Used);
            listing.UpdateMileage(10000 + (i * 500));
            listings.Add(listing);
        }

        var reportData = new ReportData
        {
            Title = "Large Dataset Report",
            Listings = listings
        };

        // Act
        var result = await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        Assert.True(File.Exists(result));
        var fileInfo = new FileInfo(result);
        Assert.True(fileInfo.Length > 0);
        // PDF should be reasonably sized even with many listings
        Assert.True(fileInfo.Length < 5 * 1024 * 1024); // Less than 5MB
    }

    [Fact]
    public async Task GenerateReportAsync_WithEmptyListings_ShouldCreateValidPdf()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "empty-report.pdf");
        var reportData = new ReportData
        {
            Title = "Empty Report",
            Listings = new List<Listing>()
        };

        // Act
        var result = await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        Assert.True(File.Exists(result));
        var fileInfo = new FileInfo(result);
        Assert.True(fileInfo.Length > 0);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
