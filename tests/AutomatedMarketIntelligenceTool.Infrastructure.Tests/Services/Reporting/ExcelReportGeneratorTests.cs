using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Reporting;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Reporting;

public class ExcelReportGeneratorTests
{
    private readonly ExcelReportGenerator _generator;
    private readonly string _tempDirectory;

    public ExcelReportGeneratorTests()
    {
        _generator = new ExcelReportGenerator(NullLogger<ExcelReportGenerator>.Instance);
        _tempDirectory = Path.Combine(Path.GetTempPath(), "excel-report-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void SupportedFormat_ShouldBeExcel()
    {
        // Assert
        Assert.Equal(ReportFormat.Excel, _generator.SupportedFormat);
    }

    [Fact]
    public async Task GenerateReportAsync_WithBasicData_ShouldCreateExcelFile()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test-report.xlsx");
        var reportData = new ReportData
        {
            Title = "Test Excel Report",
            GeneratedAt = DateTime.UtcNow,
            Listings = new List<Listing>()
        };

        // Act
        var result = await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));
        
        // Verify it's a valid Excel file by opening it
        using var workbook = new XLWorkbook(outputPath);
        Assert.NotEmpty(workbook.Worksheets);
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldCreateMultipleSheets()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "multi-sheet-report.xlsx");
        var tenantId = Guid.NewGuid();
        
        var listing = Listing.Create(tenantId, "EXT-001", "autotrader",
            "https://example.com/1", "Toyota", "Camry", 2022, 25000m, Condition.Used);

        var reportData = new ReportData
        {
            Title = "Multi-Sheet Report",
            Listings = new List<Listing> { listing },
            Statistics = new MarketStatistics { TotalListings = 1 }
        };

        // Act
        await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        using var workbook = new XLWorkbook(outputPath);
        Assert.Contains(workbook.Worksheets, ws => ws.Name == "Summary");
        Assert.Contains(workbook.Worksheets, ws => ws.Name == "Listings");
        Assert.Contains(workbook.Worksheets, ws => ws.Name == "Statistics");
    }

    [Fact]
    public async Task GenerateReportAsync_ListingsSheet_ShouldContainHeaders()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "headers-report.xlsx");
        var reportData = new ReportData
        {
            Title = "Headers Test",
            Listings = new List<Listing>()
        };

        // Act
        await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        using var workbook = new XLWorkbook(outputPath);
        var listingsSheet = workbook.Worksheet("Listings");
        
        Assert.Equal("Year", listingsSheet.Cell(1, 1).Value.ToString());
        Assert.Equal("Make", listingsSheet.Cell(1, 2).Value.ToString());
        Assert.Equal("Model", listingsSheet.Cell(1, 3).Value.ToString());
        Assert.Equal("Trim", listingsSheet.Cell(1, 4).Value.ToString());
        Assert.Equal("Price", listingsSheet.Cell(1, 5).Value.ToString());
    }

    [Fact]
    public async Task GenerateReportAsync_WithListings_ShouldPopulateData()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "data-report.xlsx");
        var tenantId = Guid.NewGuid();
        
        var listing1 = Listing.Create(tenantId, "EXT-001", "autotrader",
            "https://example.com/1", "Toyota", "Camry", 2022, 25000m, Condition.Used);
        listing1.UpdateMileage(15000);
        listing1.UpdateLocation("Los Angeles, CA", 34.0522m, -118.2437m);

        var listing2 = Listing.Create(tenantId, "EXT-002", "cargurus",
            "https://example.com/2", "Honda", "Civic", 2023, 22000m, Condition.New);
        listing2.UpdateMileage(100);

        var reportData = new ReportData
        {
            Title = "Data Report",
            Listings = new List<Listing> { listing1, listing2 }
        };

        // Act
        await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        using var workbook = new XLWorkbook(outputPath);
        var listingsSheet = workbook.Worksheet("Listings");
        
        // Check first listing (row 2, after header)
        Assert.Equal(2022, listingsSheet.Cell(2, 1).Value);
        Assert.Equal("Toyota", listingsSheet.Cell(2, 2).Value.ToString());
        Assert.Equal("Camry", listingsSheet.Cell(2, 3).Value.ToString());
        Assert.Equal(25000, listingsSheet.Cell(2, 5).Value);
        
        // Check second listing (row 3)
        Assert.Equal(2023, listingsSheet.Cell(3, 1).Value);
        Assert.Equal("Honda", listingsSheet.Cell(3, 2).Value.ToString());
        Assert.Equal("Civic", listingsSheet.Cell(3, 3).Value.ToString());
        Assert.Equal(22000, listingsSheet.Cell(3, 5).Value);
    }

    [Fact]
    public async Task GenerateReportAsync_WithStatistics_ShouldPopulateStatsSheet()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "stats-report.xlsx");
        var reportData = new ReportData
        {
            Title = "Statistics Report",
            Statistics = new MarketStatistics
            {
                TotalListings = 150,
                AveragePrice = 28500m,
                MedianPrice = 27000m,
                MinPrice = 15000m,
                MaxPrice = 45000m,
                AverageMileage = 25000
            }
        };

        // Act
        await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        using var workbook = new XLWorkbook(outputPath);
        var statsSheet = workbook.Worksheet("Statistics");
        Assert.NotNull(statsSheet);
        
        // The sheet should contain statistics data
        var cellValues = new List<string>();
        for (int row = 1; row <= 15; row++)
        {
            var cellValue = statsSheet.Cell(row, 1).Value.ToString();
            if (!string.IsNullOrEmpty(cellValue))
                cellValues.Add(cellValue);
        }
        
        Assert.Contains(cellValues, v => v.Contains("Total Listings") || v == "150");
    }

    [Fact]
    public async Task GenerateReportAsync_SummarySheet_ShouldContainMetadata()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "summary-report.xlsx");
        var reportData = new ReportData
        {
            Title = "Test Summary Report",
            SearchCriteria = "Make: Toyota\nPrice: $20,000 - $30,000"
        };

        // Act
        await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        using var workbook = new XLWorkbook(outputPath);
        var summarySheet = workbook.Worksheet("Summary");
        
        var titleCell = summarySheet.Cell(1, 1).Value.ToString();
        Assert.Contains("Test Summary Report", titleCell);
    }

    [Fact]
    public async Task GenerateReportAsync_WithEmptyListings_ShouldCreateValidExcel()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "empty-report.xlsx");
        var reportData = new ReportData
        {
            Title = "Empty Report",
            Listings = new List<Listing>()
        };

        // Act
        var result = await _generator.GenerateReportAsync(reportData, outputPath);

        // Assert
        Assert.True(File.Exists(result));
        using var workbook = new XLWorkbook(outputPath);
        Assert.NotEmpty(workbook.Worksheets);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
