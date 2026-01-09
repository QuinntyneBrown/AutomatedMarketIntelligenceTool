using System.Text;
using System.Text.Json;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Core;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Import;

public class DataImportServiceTests : IDisposable
{
    private readonly Mock<IDuplicateDetectionService> _mockDuplicateDetection;
    private readonly ILogger<DataImportService> _logger;
    private readonly AutomatedMarketIntelligenceToolContext _context;
    private readonly DataImportService _service;
    private readonly Guid _testTenantId;
    private readonly string _testDirectory;

    public DataImportServiceTests()
    {
        _mockDuplicateDetection = new Mock<IDuplicateDetectionService>();
        _logger = new LoggerFactory().CreateLogger<DataImportService>();
        _testTenantId = Guid.NewGuid();
        _testDirectory = Path.Combine(Path.GetTempPath(), "DataImportServiceTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<AutomatedMarketIntelligenceToolContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AutomatedMarketIntelligenceToolContext(options);
        _service = new DataImportService(_context, _mockDuplicateDetection.Object, _logger);
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DataImportService(null!, _mockDuplicateDetection.Object, _logger));
    }

    [Fact]
    public void Constructor_WithNullDuplicateDetection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DataImportService(_context, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DataImportService(_context, _mockDuplicateDetection.Object, null!));
    }

    [Fact]
    public async Task ImportFromCsvAsync_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.csv");

        // Act
        var result = await _service.ImportFromCsvAsync(nonExistentPath, _testTenantId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ErrorCount);
        Assert.Contains("File not found", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task ImportFromCsvAsync_WithValidData_ShouldImportSuccessfully()
    {
        // Arrange
        var csvContent = @"ExternalId,SourceSite,ListingUrl,Make,Model,Year,Price,Condition
TEST001,TestSite,http://test.com/1,Toyota,Camry,2020,25000,Used
TEST002,TestSite,http://test.com/2,Honda,Civic,2021,22000,Used";

        var csvPath = Path.Combine(_testDirectory, "test.csv");
        await File.WriteAllTextAsync(csvPath, csvContent);

        // Act
        var result = await _service.ImportFromCsvAsync(csvPath, _testTenantId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(0, result.SkippedCount);
        
        // Verify data was added to context
        var listings = await _context.Listings.ToListAsync();
        Assert.Equal(2, listings.Count);
        Assert.Contains(listings, l => l.ExternalId == "TEST001");
        Assert.Contains(listings, l => l.ExternalId == "TEST002");
    }

    [Fact]
    public async Task ImportFromCsvAsync_WithDuplicateData_ShouldSkipDuplicate()
    {
        // Arrange
        var existingListing = Listing.Create(
            tenantId: _testTenantId,
            externalId: "TEST001",
            sourceSite: "TestSite",
            listingUrl: "http://test.com/1",
            make: "Toyota",
            model: "Camry",
            year: 2020,
            price: 25000,
            condition: Core.Models.ListingAggregate.Enums.Condition.Used
        );
        _context.Listings.Add(existingListing);
        await _context.SaveChangesAsync();

        var csvContent = @"ExternalId,SourceSite,ListingUrl,Make,Model,Year,Price,Condition
TEST001,TestSite,http://test.com/1,Toyota,Camry,2020,25000,Used
TEST002,TestSite,http://test.com/2,Honda,Civic,2021,22000,Used";

        var csvPath = Path.Combine(_testDirectory, "test.csv");
        await File.WriteAllTextAsync(csvPath, csvContent);

        // Act
        var result = await _service.ImportFromCsvAsync(csvPath, _testTenantId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public async Task ImportFromCsvAsync_WithInvalidData_ShouldReportError()
    {
        // Arrange
        var csvContent = @"ExternalId,SourceSite,ListingUrl,Make,Model,Year,Price,Condition
TEST001,TestSite,http://test.com/1,Toyota,Camry,2020,25000,Used
,TestSite,http://test.com/2,Honda,Civic,2021,22000,Used";

        var csvPath = Path.Combine(_testDirectory, "test.csv");
        await File.WriteAllTextAsync(csvPath, csvContent);

        // Act
        var result = await _service.ImportFromCsvAsync(csvPath, _testTenantId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Contains("ExternalId is required", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task ImportFromCsvAsync_WithDryRun_ShouldNotSaveData()
    {
        // Arrange
        var csvContent = @"ExternalId,SourceSite,ListingUrl,Make,Model,Year,Price,Condition
TEST001,TestSite,http://test.com/1,Toyota,Camry,2020,25000,Used";

        var csvPath = Path.Combine(_testDirectory, "test.csv");
        await File.WriteAllTextAsync(csvPath, csvContent);

        // Act
        var result = await _service.ImportFromCsvAsync(csvPath, _testTenantId, dryRun: true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsDryRun);
        Assert.Equal(1, result.SuccessCount);
        
        // Verify no data was saved
        var listings = await _context.Listings.ToListAsync();
        Assert.Empty(listings);
    }

    [Fact]
    public async Task ImportFromJsonAsync_WithValidData_ShouldImportSuccessfully()
    {
        // Arrange
        var jsonData = new[]
        {
            new
            {
                ExternalId = "TEST001",
                SourceSite = "TestSite",
                ListingUrl = "http://test.com/1",
                Make = "Toyota",
                Model = "Camry",
                Year = 2020,
                Price = 25000m,
                Condition = "Used"
            },
            new
            {
                ExternalId = "TEST002",
                SourceSite = "TestSite",
                ListingUrl = "http://test.com/2",
                Make = "Honda",
                Model = "Civic",
                Year = 2021,
                Price = 22000m,
                Condition = "Used"
            }
        };

        var jsonPath = Path.Combine(_testDirectory, "test.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(jsonData));

        // Act
        var result = await _service.ImportFromJsonAsync(jsonPath, _testTenantId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public async Task ImportFromJsonAsync_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

        // Act
        var result = await _service.ImportFromJsonAsync(nonExistentPath, _testTenantId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ErrorCount);
        Assert.Contains("File not found", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task ValidateImportFileAsync_WithValidCsv_ShouldReturnValid()
    {
        // Arrange
        var csvContent = @"ExternalId,SourceSite,ListingUrl,Make,Model,Year,Price,Condition
TEST001,TestSite,http://test.com/1,Toyota,Camry,2020,25000,Used";

        var csvPath = Path.Combine(_testDirectory, "test.csv");
        await File.WriteAllTextAsync(csvPath, csvContent);

        // Act
        var result = await _service.ValidateImportFileAsync(csvPath, ImportFormat.Csv);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.EstimatedRowCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateImportFileAsync_WithValidJson_ShouldReturnValid()
    {
        // Arrange
        var jsonData = new[]
        {
            new { ExternalId = "TEST001", SourceSite = "TestSite", ListingUrl = "http://test.com/1", 
                  Make = "Toyota", Model = "Camry", Year = 2020, Price = 25000m, Condition = "Used" }
        };

        var jsonPath = Path.Combine(_testDirectory, "test.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(jsonData));

        // Act
        var result = await _service.ValidateImportFileAsync(jsonPath, ImportFormat.Json);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.EstimatedRowCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateImportFileAsync_WithNonExistentFile_ShouldReturnInvalid()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.csv");

        // Act
        var result = await _service.ValidateImportFileAsync(nonExistentPath, ImportFormat.Csv);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("File not found", result.Errors[0]);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        
        _context?.Dispose();
    }
}
