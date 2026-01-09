using System.Globalization;
using System.Text.Json;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Import;

public class DataImportService : IDataImportService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly IDuplicateDetectionService _duplicateDetection;
    private readonly ILogger<DataImportService> _logger;

    public DataImportService(
        IAutomatedMarketIntelligenceToolContext context,
        IDuplicateDetectionService duplicateDetection,
        ILogger<DataImportService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _duplicateDetection = duplicateDetection ?? throw new ArgumentNullException(nameof(duplicateDetection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ImportResult> ImportFromCsvAsync(string filePath, Guid tenantId, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new ImportResult { IsDryRun = dryRun };

        if (!File.Exists(filePath))
        {
            result.Errors.Add(new ImportError { LineNumber = 0, ErrorMessage = $"File not found: {filePath}" });
            result.ErrorCount = 1;
            return result;
        }

        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);

            csv.Context.RegisterClassMap<ListingCsvMap>();
            var records = csv.GetRecords<ListingCsvRecord>().ToList();
            result.TotalRows = records.Count;

            var lineNumber = 2; // Start from 2 (1 is header)
            foreach (var record in records)
            {
                try
                {
                    var validationError = ValidateRecord(record);
                    if (validationError != null)
                    {
                        result.Errors.Add(new ImportError
                        {
                            LineNumber = lineNumber,
                            ErrorMessage = validationError
                        });
                        result.ErrorCount++;
                        lineNumber++;
                        continue;
                    }

                    // Check for duplicates (ignore query filters to check across all tenants)
                    var existingListing = await _context.Listings
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(l => l.TenantId == tenantId &&
                                                  l.ExternalId == record.ExternalId &&
                                                  l.SourceSite == record.SourceSite,
                            cancellationToken);

                    if (existingListing != null)
                    {
                        result.SkippedCount++;
                        _logger.LogDebug("Skipping duplicate listing: {ExternalId} from {SourceSite}", record.ExternalId, record.SourceSite);
                        lineNumber++;
                        continue;
                    }

                    if (!dryRun)
                    {
                        var listing = CreateListingFromRecord(record, tenantId);
                        _context.Listings.Add(listing);
                    }

                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing line {LineNumber}", lineNumber);
                    result.Errors.Add(new ImportError
                    {
                        LineNumber = lineNumber,
                        ErrorMessage = ex.Message
                    });
                    result.ErrorCount++;
                }

                lineNumber++;
            }

            if (!dryRun && result.SuccessCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading CSV file: {FilePath}", filePath);
            result.Errors.Add(new ImportError { LineNumber = 0, ErrorMessage = $"File read error: {ex.Message}" });
            result.ErrorCount++;
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    public async Task<ImportResult> ImportFromJsonAsync(string filePath, Guid tenantId, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new ImportResult { IsDryRun = dryRun };

        if (!File.Exists(filePath))
        {
            result.Errors.Add(new ImportError { LineNumber = 0, ErrorMessage = $"File not found: {filePath}" });
            result.ErrorCount = 1;
            return result;
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            var records = JsonSerializer.Deserialize<List<ListingJsonRecord>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (records == null)
            {
                result.Errors.Add(new ImportError { LineNumber = 0, ErrorMessage = "Failed to parse JSON file" });
                result.ErrorCount = 1;
                return result;
            }

            result.TotalRows = records.Count;

            var lineNumber = 1;
            foreach (var record in records)
            {
                try
                {
                    var validationError = ValidateJsonRecord(record);
                    if (validationError != null)
                    {
                        result.Errors.Add(new ImportError
                        {
                            LineNumber = lineNumber,
                            ErrorMessage = validationError
                        });
                        result.ErrorCount++;
                        lineNumber++;
                        continue;
                    }

                    // Check for duplicates (ignore query filters to check across all tenants)
                    var existingListing = await _context.Listings
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(l => l.TenantId == tenantId &&
                                                  l.ExternalId == record.ExternalId &&
                                                  l.SourceSite == record.SourceSite,
                            cancellationToken);

                    if (existingListing != null)
                    {
                        result.SkippedCount++;
                        _logger.LogDebug("Skipping duplicate listing: {ExternalId} from {SourceSite}", record.ExternalId, record.SourceSite);
                        lineNumber++;
                        continue;
                    }

                    if (!dryRun)
                    {
                        var listing = CreateListingFromJsonRecord(record, tenantId);
                        _context.Listings.Add(listing);
                    }

                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing record {LineNumber}", lineNumber);
                    result.Errors.Add(new ImportError
                    {
                        LineNumber = lineNumber,
                        ErrorMessage = ex.Message
                    });
                    result.ErrorCount++;
                }

                lineNumber++;
            }

            if (!dryRun && result.SuccessCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading JSON file: {FilePath}", filePath);
            result.Errors.Add(new ImportError { LineNumber = 0, ErrorMessage = $"File read error: {ex.Message}" });
            result.ErrorCount++;
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    public async Task<ValidationResult> ValidateImportFileAsync(string filePath, ImportFormat format, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult { IsValid = true };

        if (!File.Exists(filePath))
        {
            result.IsValid = false;
            result.Errors.Add($"File not found: {filePath}");
            return result;
        }

        try
        {
            if (format == ImportFormat.Csv)
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true
                };

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, config);
                
                csv.Context.RegisterClassMap<ListingCsvMap>();
                var records = csv.GetRecords<ListingCsvRecord>().ToList();
                result.EstimatedRowCount = records.Count;
            }
            else if (format == ImportFormat.Json)
            {
                var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
                var records = JsonSerializer.Deserialize<List<ListingJsonRecord>>(jsonContent);
                
                if (records == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("Invalid JSON format");
                }
                else
                {
                    result.EstimatedRowCount = records.Count;
                }
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    private string? ValidateRecord(ListingCsvRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.ExternalId))
            return "ExternalId is required";
        if (string.IsNullOrWhiteSpace(record.SourceSite))
            return "SourceSite is required";
        if (string.IsNullOrWhiteSpace(record.ListingUrl))
            return "ListingUrl is required";
        if (string.IsNullOrWhiteSpace(record.Make))
            return "Make is required";
        if (string.IsNullOrWhiteSpace(record.Model))
            return "Model is required";
        if (record.Year < 1900 || record.Year > DateTime.UtcNow.Year + 1)
            return $"Year must be between 1900 and {DateTime.UtcNow.Year + 1}";
        if (record.Price <= 0)
            return "Price must be greater than 0";

        return null;
    }

    private string? ValidateJsonRecord(ListingJsonRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.ExternalId))
            return "ExternalId is required";
        if (string.IsNullOrWhiteSpace(record.SourceSite))
            return "SourceSite is required";
        if (string.IsNullOrWhiteSpace(record.ListingUrl))
            return "ListingUrl is required";
        if (string.IsNullOrWhiteSpace(record.Make))
            return "Make is required";
        if (string.IsNullOrWhiteSpace(record.Model))
            return "Model is required";
        if (record.Year < 1900 || record.Year > DateTime.UtcNow.Year + 1)
            return $"Year must be between 1900 and {DateTime.UtcNow.Year + 1}";
        if (record.Price <= 0)
            return "Price must be greater than 0";

        return null;
    }

    private Listing CreateListingFromRecord(ListingCsvRecord record, Guid tenantId)
    {
        var condition = ParseEnum<Condition>(record.Condition, Condition.Used);
        var transmission = ParseEnumNullable<Transmission>(record.Transmission);
        var fuelType = ParseEnumNullable<FuelType>(record.FuelType);
        var bodyStyle = ParseEnumNullable<BodyStyle>(record.BodyStyle);
        var drivetrain = ParseEnumNullable<Drivetrain>(record.Drivetrain);
        var sellerType = ParseEnumNullable<SellerType>(record.SellerType);

        return Listing.Create(
            tenantId: tenantId,
            externalId: record.ExternalId,
            sourceSite: record.SourceSite,
            listingUrl: record.ListingUrl,
            make: record.Make,
            model: record.Model,
            year: record.Year,
            price: record.Price,
            condition: condition,
            trim: record.Trim,
            mileage: record.Mileage,
            vin: record.Vin,
            city: record.City,
            province: record.Province,
            postalCode: record.PostalCode,
            currency: record.Currency ?? "CAD",
            transmission: transmission,
            fuelType: fuelType,
            bodyStyle: bodyStyle,
            drivetrain: drivetrain,
            exteriorColor: record.ExteriorColor,
            interiorColor: record.InteriorColor,
            sellerType: sellerType,
            sellerName: record.SellerName,
            sellerPhone: record.SellerPhone,
            description: record.Description,
            imageUrls: ParseImageUrls(record.ImageUrls),
            listingDate: record.ListingDate,
            daysOnMarket: record.DaysOnMarket
        );
    }

    private Listing CreateListingFromJsonRecord(ListingJsonRecord record, Guid tenantId)
    {
        var condition = ParseEnum<Condition>(record.Condition, Condition.Used);
        var transmission = ParseEnumNullable<Transmission>(record.Transmission);
        var fuelType = ParseEnumNullable<FuelType>(record.FuelType);
        var bodyStyle = ParseEnumNullable<BodyStyle>(record.BodyStyle);
        var drivetrain = ParseEnumNullable<Drivetrain>(record.Drivetrain);
        var sellerType = ParseEnumNullable<SellerType>(record.SellerType);

        return Listing.Create(
            tenantId: tenantId,
            externalId: record.ExternalId,
            sourceSite: record.SourceSite,
            listingUrl: record.ListingUrl,
            make: record.Make,
            model: record.Model,
            year: record.Year,
            price: record.Price,
            condition: condition,
            trim: record.Trim,
            mileage: record.Mileage,
            vin: record.Vin,
            city: record.City,
            province: record.Province,
            postalCode: record.PostalCode,
            currency: record.Currency ?? "CAD",
            transmission: transmission,
            fuelType: fuelType,
            bodyStyle: bodyStyle,
            drivetrain: drivetrain,
            exteriorColor: record.ExteriorColor,
            interiorColor: record.InteriorColor,
            sellerType: sellerType,
            sellerName: record.SellerName,
            sellerPhone: record.SellerPhone,
            description: record.Description,
            imageUrls: record.ImageUrls ?? new List<string>(),
            listingDate: record.ListingDate,
            daysOnMarket: record.DaysOnMarket
        );
    }

    private T ParseEnum<T>(string? value, T defaultValue) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return Enum.TryParse<T>(value, true, out var result) ? result : defaultValue;
    }

    private T? ParseEnumNullable<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Enum.TryParse<T>(value, true, out var result) ? result : null;
    }

    private List<string> ParseImageUrls(string? imageUrls)
    {
        if (string.IsNullOrWhiteSpace(imageUrls))
            return new List<string>();

        // Handle both pipe-separated and JSON array formats
        if (imageUrls.TrimStart().StartsWith("["))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(imageUrls) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        return imageUrls.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(url => url.Trim())
            .ToList();
    }
}

// CSV Record class
public class ListingCsvRecord
{
    public string ExternalId { get; set; } = string.Empty;
    public string SourceSite { get; set; } = string.Empty;
    public string ListingUrl { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? Trim { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public int? Mileage { get; set; }
    public string? Vin { get; set; }
    public string Condition { get; set; } = "Used";
    public string? Transmission { get; set; }
    public string? FuelType { get; set; }
    public string? BodyStyle { get; set; }
    public string? Drivetrain { get; set; }
    public string? ExteriorColor { get; set; }
    public string? InteriorColor { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? SellerType { get; set; }
    public string? SellerName { get; set; }
    public string? SellerPhone { get; set; }
    public string? Description { get; set; }
    public string? ImageUrls { get; set; }
    public DateTime? ListingDate { get; set; }
    public int? DaysOnMarket { get; set; }
}

// JSON Record class
public class ListingJsonRecord
{
    public string ExternalId { get; set; } = string.Empty;
    public string SourceSite { get; set; } = string.Empty;
    public string ListingUrl { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? Trim { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public int? Mileage { get; set; }
    public string? Vin { get; set; }
    public string Condition { get; set; } = "Used";
    public string? Transmission { get; set; }
    public string? FuelType { get; set; }
    public string? BodyStyle { get; set; }
    public string? Drivetrain { get; set; }
    public string? ExteriorColor { get; set; }
    public string? InteriorColor { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? SellerType { get; set; }
    public string? SellerName { get; set; }
    public string? SellerPhone { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public DateTime? ListingDate { get; set; }
    public int? DaysOnMarket { get; set; }
}

// CSV Mapping
public class ListingCsvMap : ClassMap<ListingCsvRecord>
{
    public ListingCsvMap()
    {
        Map(m => m.ExternalId).Name("ExternalId");
        Map(m => m.SourceSite).Name("SourceSite");
        Map(m => m.ListingUrl).Name("ListingUrl");
        Map(m => m.Make).Name("Make");
        Map(m => m.Model).Name("Model");
        Map(m => m.Year).Name("Year");
        Map(m => m.Trim).Name("Trim").Optional();
        Map(m => m.Price).Name("Price");
        Map(m => m.Currency).Name("Currency").Optional();
        Map(m => m.Mileage).Name("Mileage").Optional();
        Map(m => m.Vin).Name("VIN").Optional();
        Map(m => m.Condition).Name("Condition").Optional();
        Map(m => m.Transmission).Name("Transmission").Optional();
        Map(m => m.FuelType).Name("FuelType").Optional();
        Map(m => m.BodyStyle).Name("BodyStyle").Optional();
        Map(m => m.Drivetrain).Name("Drivetrain").Optional();
        Map(m => m.ExteriorColor).Name("ExteriorColor").Optional();
        Map(m => m.InteriorColor).Name("InteriorColor").Optional();
        Map(m => m.City).Name("City").Optional();
        Map(m => m.Province).Name("Province").Optional();
        Map(m => m.PostalCode).Name("PostalCode").Optional();
        Map(m => m.SellerType).Name("SellerType").Optional();
        Map(m => m.SellerName).Name("SellerName").Optional();
        Map(m => m.SellerPhone).Name("SellerPhone").Optional();
        Map(m => m.Description).Name("Description").Optional();
        Map(m => m.ImageUrls).Name("ImageUrls").Optional();
        Map(m => m.ListingDate).Name("ListingDate").Optional();
        Map(m => m.DaysOnMarket).Name("DaysOnMarket").Optional();
    }
}
