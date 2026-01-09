using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to export car listings to JSON or CSV files.
/// </summary>
public class ExportCommand : AsyncCommand<ExportCommand.Settings>
{
    private readonly ISearchService _searchService;

    public ExportCommand(ISearchService searchService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Determine format from file extension or explicit format parameter
            var format = DetermineFormat(settings.OutputFile, settings.Format);
            if (format == null)
            {
                AnsiConsole.MarkupLine("[red]Error: Could not determine export format. Please specify --format or use a .json or .csv file extension.[/]");
                return ExitCodes.ValidationError;
            }

            // Check if file exists and confirm overwrite
            if (File.Exists(settings.OutputFile))
            {
                if (!AnsiConsole.Confirm($"File '{settings.OutputFile}' already exists. Overwrite?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Export cancelled.[/]");
                    return ExitCodes.Success;
                }
            }

            // Parse enums from string arrays
            Condition[]? conditions = null;
            if (settings.Conditions?.Length > 0)
            {
                conditions = ParseEnums<Condition>(settings.Conditions, "condition");
                if (conditions == null) return ExitCodes.ValidationError;
            }

            Transmission[]? transmissions = null;
            if (settings.Transmissions?.Length > 0)
            {
                transmissions = ParseEnums<Transmission>(settings.Transmissions, "transmission");
                if (transmissions == null) return ExitCodes.ValidationError;
            }

            FuelType[]? fuelTypes = null;
            if (settings.FuelTypes?.Length > 0)
            {
                fuelTypes = ParseEnums<FuelType>(settings.FuelTypes, "fuel type");
                if (fuelTypes == null) return ExitCodes.ValidationError;
            }

            BodyStyle[]? bodyStyles = null;
            if (settings.BodyStyles?.Length > 0)
            {
                bodyStyles = ParseEnums<BodyStyle>(settings.BodyStyles, "body style");
                if (bodyStyles == null) return ExitCodes.ValidationError;
            }

            SearchSortField? sortBy = null;
            if (!string.IsNullOrEmpty(settings.SortBy))
            {
                if (!Enum.TryParse<SearchSortField>(settings.SortBy, true, out var parsedSort))
                {
                    AnsiConsole.MarkupLine($"[red]Error: Invalid sort field '{settings.SortBy}'. Valid values are: Price, Year, Mileage, Distance, CreatedAt[/]");
                    return ExitCodes.ValidationError;
                }
                sortBy = parsedSort;
            }

            var sortDirection = settings.SortDescending ? SortDirection.Descending : SortDirection.Ascending;

            // Build search criteria - collect all pages
            var criteria = new SearchCriteria
            {
                TenantId = settings.TenantId,
                Makes = settings.Makes,
                Models = settings.Models,
                YearMin = settings.YearMin,
                YearMax = settings.YearMax,
                PriceMin = settings.PriceMin,
                PriceMax = settings.PriceMax,
                MileageMin = settings.MileageMin,
                MileageMax = settings.MileageMax,
                Conditions = conditions,
                Transmissions = transmissions,
                FuelTypes = fuelTypes,
                BodyStyles = bodyStyles,
                City = settings.City,
                Province = settings.State,
                PostalCode = settings.PostalCode,
                RadiusKilometers = settings.Radius,
                Page = 1,
                PageSize = 1000, // Large page size for export
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            // Execute search with progress indicator
            var allListings = new List<ListingSearchResult>();
            var currentPage = 1;
            
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Fetching listings for export...[/]");
                    
                    SearchResult result;
                    do
                    {
                        var pageCriteria = new SearchCriteria
                        {
                            TenantId = criteria.TenantId,
                            Makes = criteria.Makes,
                            Models = criteria.Models,
                            YearMin = criteria.YearMin,
                            YearMax = criteria.YearMax,
                            PriceMin = criteria.PriceMin,
                            PriceMax = criteria.PriceMax,
                            MileageMin = criteria.MileageMin,
                            MileageMax = criteria.MileageMax,
                            Conditions = criteria.Conditions,
                            Transmissions = criteria.Transmissions,
                            FuelTypes = criteria.FuelTypes,
                            BodyStyles = criteria.BodyStyles,
                            City = criteria.City,
                            Province = criteria.Province,
                            PostalCode = criteria.PostalCode,
                            RadiusKilometers = criteria.RadiusKilometers,
                            Page = currentPage,
                            PageSize = criteria.PageSize,
                            SortBy = criteria.SortBy,
                            SortDirection = criteria.SortDirection
                        };
                        
                        result = await _searchService.SearchListingsAsync(pageCriteria);
                        allListings.AddRange(result.Listings);
                        
                        task.Value = (double)allListings.Count / result.TotalCount * 100;
                        task.Description = $"[green]Fetched {allListings.Count} of {result.TotalCount} listings...[/]";
                        
                        currentPage++;
                    } while (currentPage <= result.TotalPages);
                    
                    task.Value = 100;
                });

            if (allListings.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No listings found matching the criteria.[/]");
                return ExitCodes.Success;
            }

            // Export based on format
            await AnsiConsole.Status()
                .StartAsync($"Exporting to {format.ToUpper()}...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    
                    if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
                    {
                        await ExportToJsonAsync(settings.OutputFile, allListings);
                    }
                    else if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                    {
                        await ExportToCsvAsync(settings.OutputFile, allListings);
                    }
                });

            AnsiConsole.MarkupLine($"[green]Successfully exported {allListings.Count} listings to '{settings.OutputFile}'[/]");
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.GeneralError;
        }
    }

    private string? DetermineFormat(string outputFile, string? explicitFormat)
    {
        if (!string.IsNullOrEmpty(explicitFormat))
        {
            return explicitFormat.ToLower();
        }

        var extension = Path.GetExtension(outputFile).ToLower();
        return extension switch
        {
            ".json" => "json",
            ".csv" => "csv",
            _ => null
        };
    }

    private async Task ExportToJsonAsync(string outputFile, List<ListingSearchResult> listings)
    {
        var exportData = listings.Select(lr => new
        {
            ListingId = lr.Listing.ListingId.Value,
            ExternalId = lr.Listing.ExternalId,
            SourceSite = lr.Listing.SourceSite,
            ListingUrl = lr.Listing.ListingUrl,
            Year = lr.Listing.Year,
            Make = lr.Listing.Make,
            Model = lr.Listing.Model,
            Trim = lr.Listing.Trim,
            Price = lr.Listing.Price,
            Currency = lr.Listing.Currency,
            Mileage = lr.Listing.Mileage,
            Vin = lr.Listing.Vin,
            Condition = lr.Listing.Condition.ToString(),
            Transmission = lr.Listing.Transmission?.ToString(),
            FuelType = lr.Listing.FuelType?.ToString(),
            BodyStyle = lr.Listing.BodyStyle?.ToString(),
            Drivetrain = lr.Listing.Drivetrain?.ToString(),
            ExteriorColor = lr.Listing.ExteriorColor,
            InteriorColor = lr.Listing.InteriorColor,
            City = lr.Listing.City,
            Province = lr.Listing.Province,
            PostalCode = lr.Listing.PostalCode,
            DistanceKm = lr.DistanceKilometers,
            SellerType = lr.Listing.SellerType?.ToString(),
            SellerName = lr.Listing.SellerName,
            SellerPhone = lr.Listing.SellerPhone,
            Description = lr.Listing.Description,
            ImageUrls = lr.Listing.ImageUrls,
            ListingDate = lr.Listing.ListingDate,
            DaysOnMarket = lr.Listing.DaysOnMarket,
            FirstSeenDate = lr.Listing.FirstSeenDate,
            LastSeenDate = lr.Listing.LastSeenDate,
            IsNewListing = lr.Listing.IsNewListing,
            IsActive = lr.Listing.IsActive,
            CreatedAt = lr.Listing.CreatedAt,
            PriceChange = lr.PriceChange != null ? new
            {
                PreviousPrice = lr.PriceChange.PreviousPrice,
                CurrentPrice = lr.PriceChange.CurrentPrice,
                Change = lr.PriceChange.PriceChange,
                ChangePercentage = lr.PriceChange.ChangePercentage,
                ChangedAt = lr.PriceChange.ChangedAt
            } : null
        });

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(exportData, options);
        await File.WriteAllTextAsync(outputFile, json);
    }

    private async Task ExportToCsvAsync(string outputFile, List<ListingSearchResult> listings)
    {
        using var writer = new StreamWriter(outputFile);
        
        // Write header
        await writer.WriteLineAsync("Year,Make,Model,Trim,Price,Currency,Mileage,Condition,Transmission,FuelType,BodyStyle,Drivetrain,ExteriorColor,InteriorColor,City,Province,PostalCode,DistanceKm,SellerType,SellerName,SellerPhone,VIN,ExternalId,SourceSite,ListingUrl,ListingDate,DaysOnMarket,FirstSeenDate,LastSeenDate,IsNewListing,IsActive,PreviousPrice,PriceChange,ChangePercentage");

        // Write rows
        foreach (var lr in listings)
        {
            var row = string.Join(",", new[]
            {
                lr.Listing.Year.ToString(),
                EscapeCsv(lr.Listing.Make),
                EscapeCsv(lr.Listing.Model),
                EscapeCsv(lr.Listing.Trim),
                lr.Listing.Price.ToString("F2"),
                lr.Listing.Currency,
                lr.Listing.Mileage?.ToString() ?? "",
                lr.Listing.Condition.ToString(),
                lr.Listing.Transmission?.ToString() ?? "",
                lr.Listing.FuelType?.ToString() ?? "",
                lr.Listing.BodyStyle?.ToString() ?? "",
                lr.Listing.Drivetrain?.ToString() ?? "",
                EscapeCsv(lr.Listing.ExteriorColor),
                EscapeCsv(lr.Listing.InteriorColor),
                EscapeCsv(lr.Listing.City),
                EscapeCsv(lr.Listing.Province),
                EscapeCsv(lr.Listing.PostalCode),
                lr.DistanceKilometers?.ToString("F1") ?? "",
                lr.Listing.SellerType?.ToString() ?? "",
                EscapeCsv(lr.Listing.SellerName),
                EscapeCsv(lr.Listing.SellerPhone),
                EscapeCsv(lr.Listing.Vin),
                EscapeCsv(lr.Listing.ExternalId),
                EscapeCsv(lr.Listing.SourceSite),
                EscapeCsv(lr.Listing.ListingUrl),
                lr.Listing.ListingDate?.ToString("yyyy-MM-dd") ?? "",
                lr.Listing.DaysOnMarket?.ToString() ?? "",
                lr.Listing.FirstSeenDate.ToString("yyyy-MM-dd"),
                lr.Listing.LastSeenDate.ToString("yyyy-MM-dd"),
                lr.Listing.IsNewListing.ToString(),
                lr.Listing.IsActive.ToString(),
                lr.PriceChange?.PreviousPrice.ToString("F2") ?? "",
                lr.PriceChange?.PriceChange.ToString("F2") ?? "",
                lr.PriceChange?.ChangePercentage.ToString("F2") ?? ""
            });
            
            await writer.WriteLineAsync(row);
        }
    }

    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private T[]? ParseEnums<T>(string[] values, string fieldName) where T : struct, Enum
    {
        var results = new List<T>();
        foreach (var value in values)
        {
            if (!Enum.TryParse<T>(value, true, out var parsed))
            {
                var validValues = string.Join(", ", Enum.GetNames(typeof(T)));
                AnsiConsole.MarkupLine($"[red]Error: Invalid {fieldName} '{value}'. Valid values are: {validValues}[/]");
                return null;
            }
            results.Add(parsed);
        }
        return results.ToArray();
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<output-file>")]
        [Description("Output file path (.json or .csv)")]
        public string OutputFile { get; set; } = string.Empty;

        [CommandOption("-f|--format")]
        [Description("Export format: json, csv (auto-detected from extension if not specified)")]
        public string? Format { get; set; }

        [CommandOption("-t|--tenant")]
        [Description("Tenant ID (required)")]
        public Guid TenantId { get; set; }

        [CommandOption("-m|--make")]
        [Description("Vehicle make(s) (e.g., Toyota, Honda)")]
        public string[]? Makes { get; set; }

        [CommandOption("--model")]
        [Description("Vehicle model(s) (e.g., Camry, Civic)")]
        public string[]? Models { get; set; }

        [CommandOption("--year-min")]
        [Description("Minimum year")]
        public int? YearMin { get; set; }

        [CommandOption("--year-max")]
        [Description("Maximum year")]
        public int? YearMax { get; set; }

        [CommandOption("--price-min")]
        [Description("Minimum price")]
        public decimal? PriceMin { get; set; }

        [CommandOption("--price-max")]
        [Description("Maximum price")]
        public decimal? PriceMax { get; set; }

        [CommandOption("--mileage-min")]
        [Description("Minimum mileage")]
        public int? MileageMin { get; set; }

        [CommandOption("--mileage-max")]
        [Description("Maximum mileage")]
        public int? MileageMax { get; set; }

        [CommandOption("--condition")]
        [Description("Condition(s): New, Used, Certified")]
        public string[]? Conditions { get; set; }

        [CommandOption("--transmission")]
        [Description("Transmission(s): Automatic, Manual, CVT")]
        public string[]? Transmissions { get; set; }

        [CommandOption("--fuel-type")]
        [Description("Fuel type(s): Gasoline, Diesel, Electric, Hybrid, PlugInHybrid")]
        public string[]? FuelTypes { get; set; }

        [CommandOption("--body-style")]
        [Description("Body style(s): Sedan, SUV, Truck, Coupe, Hatchback, Wagon, Van, Convertible")]
        public string[]? BodyStyles { get; set; }

        [CommandOption("--city")]
        [Description("City name")]
        public string? City { get; set; }

        [CommandOption("--state")]
        [Description("Province/State")]
        public string? State { get; set; }

        [CommandOption("-p|--postal-code")]
        [Description("Postal code for location search")]
        public string? PostalCode { get; set; }

        [CommandOption("-r|--radius")]
        [Description("Search radius in kilometers (default: 40)")]
        [DefaultValue(40)]
        public double Radius { get; set; } = 40;

        [CommandOption("--sort")]
        [Description("Sort by field: Price, Year, Mileage, Distance, CreatedAt")]
        public string? SortBy { get; set; }

        [CommandOption("--sort-desc")]
        [Description("Sort in descending order")]
        [DefaultValue(false)]
        public bool SortDescending { get; set; }
    }
}
