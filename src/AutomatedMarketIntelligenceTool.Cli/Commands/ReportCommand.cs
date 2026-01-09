using System.ComponentModel;
using System.Text.Json;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Core.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to generate reports in various formats (HTML, PDF, Excel).
/// </summary>
public class ReportCommand : AsyncCommand<ReportCommand.Settings>
{
    private readonly IReportGenerationService _reportGenerationService;
    private readonly ISearchService _searchService;
    private readonly IStatisticsService _statisticsService;
    private readonly Core.IAutomatedMarketIntelligenceToolContext _context;

    public ReportCommand(
        IReportGenerationService reportGenerationService,
        ISearchService searchService,
        IStatisticsService statisticsService,
        Core.IAutomatedMarketIntelligenceToolContext context)
    {
        _reportGenerationService = reportGenerationService ?? throw new ArgumentNullException(nameof(reportGenerationService));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Validate format
            if (!Enum.TryParse<ReportFormat>(settings.Format, true, out var format))
            {
                AnsiConsole.MarkupLine($"[red]Error: Invalid format '{settings.Format}'. Valid formats are: html, pdf, excel[/]");
                return ExitCodes.ValidationError;
            }

            // Determine output path
            var outputPath = DetermineOutputPath(settings.OutputPath, format);
            
            // Ensure output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ReportData? reportData = null;

            await AnsiConsole.Status()
                .StartAsync("Gathering report data...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    reportData = await GatherReportDataAsync(settings);
                });

            if (reportData == null)
            {
                AnsiConsole.MarkupLine("[red]Error: Failed to gather report data[/]");
                return ExitCodes.GeneralError;
            }

            // Generate report
            await AnsiConsole.Status()
                .StartAsync($"Generating {format} report...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    
                    var searchCriteriaJson = BuildSearchCriteriaJson(settings);
                    
                    await _reportGenerationService.GenerateReportAsync(
                        settings.TenantId,
                        settings.Name ?? $"Market Report {DateTime.UtcNow:yyyy-MM-dd}",
                        format,
                        reportData,
                        outputPath,
                        searchCriteriaJson);
                });

            // Display success message
            var panel = new Panel($"[green]Report generated successfully![/]\n\nFormat: [yellow]{format}[/]\nPath: [blue]{outputPath}[/]\nSize: {FormatFileSize(new FileInfo(outputPath).Length)}")
            {
                Header = new PanelHeader("Report Generated"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ExitCodes.UnexpectedError;
        }
    }

    private async Task<ReportData> GatherReportDataAsync(Settings settings)
    {
        var reportData = new ReportData
        {
            Title = settings.Title ?? "Market Intelligence Report",
            GeneratedAt = DateTime.UtcNow
        };

        // Build search criteria text
        reportData.SearchCriteria = BuildSearchCriteriaText(settings);

        // Get listings
        var searchCriteria = BuildSearchCriteria(settings);
        var searchResult = await _searchService.SearchListingsAsync(searchCriteria);

        reportData.Listings = searchResult.Listings.Select(x => x.Listing).ToList();

        // Get statistics
        if (settings.IncludeStatistics)
        {
            reportData.Statistics = await _statisticsService.GetMarketStatisticsAsync(
                settings.TenantId,
                settings.Make,
                settings.Model,
                settings.YearMin,
                settings.YearMax,
                settings.PriceMin,
                settings.PriceMax,
                settings.MileageMax,
                settings.Condition,
                settings.BodyStyle);
        }

        return reportData;
    }

    private SearchCriteria BuildSearchCriteria(Settings settings)
    {
        return new SearchCriteria
        {
            TenantId = settings.TenantId,
            Makes = !string.IsNullOrEmpty(settings.Make) ? new[] { settings.Make } : null,
            Models = !string.IsNullOrEmpty(settings.Model) ? new[] { settings.Model } : null,
            YearMin = settings.YearMin,
            YearMax = settings.YearMax,
            PriceMin = settings.PriceMin,
            PriceMax = settings.PriceMax,
            MileageMin = settings.MileageMin,
            MileageMax = settings.MileageMax,
            PostalCode = settings.ZipCode,
            RadiusKilometers = settings.Radius,
            PageSize = 1000 // Get more results for report
        };
    }

    private string BuildSearchCriteriaText(Settings settings)
    {
        var criteria = new List<string>();

        if (!string.IsNullOrEmpty(settings.Make))
            criteria.Add($"Make: {settings.Make}");
        
        if (!string.IsNullOrEmpty(settings.Model))
            criteria.Add($"Model: {settings.Model}");
        
        if (settings.YearMin.HasValue || settings.YearMax.HasValue)
        {
            var yearRange = settings.YearMin.HasValue && settings.YearMax.HasValue
                ? $"{settings.YearMin}-{settings.YearMax}"
                : settings.YearMin.HasValue
                    ? $"{settings.YearMin}+"
                    : $"Up to {settings.YearMax}";
            criteria.Add($"Year: {yearRange}");
        }

        if (settings.PriceMin.HasValue || settings.PriceMax.HasValue)
        {
            var priceRange = settings.PriceMin.HasValue && settings.PriceMax.HasValue
                ? $"${settings.PriceMin:N0} - ${settings.PriceMax:N0}"
                : settings.PriceMin.HasValue
                    ? $"${settings.PriceMin:N0}+"
                    : $"Up to ${settings.PriceMax:N0}";
            criteria.Add($"Price: {priceRange}");
        }

        if (settings.MileageMax.HasValue)
            criteria.Add($"Mileage: Up to {settings.MileageMax:N0}");

        if (!string.IsNullOrEmpty(settings.ZipCode))
        {
            var location = settings.Radius.HasValue
                ? $"{settings.ZipCode} (within {settings.Radius} km)"
                : settings.ZipCode;
            criteria.Add($"Location: {location}");
        }

        if (!string.IsNullOrEmpty(settings.Condition))
            criteria.Add($"Condition: {settings.Condition}");

        if (!string.IsNullOrEmpty(settings.BodyStyle))
            criteria.Add($"Body Style: {settings.BodyStyle}");

        return criteria.Any() ? string.Join("\n", criteria) : "No filters applied";
    }

    private string BuildSearchCriteriaJson(Settings settings)
    {
        var criteria = new Dictionary<string, object?>();

        if (!string.IsNullOrEmpty(settings.Make))
            criteria["make"] = settings.Make;
        if (!string.IsNullOrEmpty(settings.Model))
            criteria["model"] = settings.Model;
        if (settings.YearMin.HasValue)
            criteria["yearMin"] = settings.YearMin;
        if (settings.YearMax.HasValue)
            criteria["yearMax"] = settings.YearMax;
        if (settings.PriceMin.HasValue)
            criteria["priceMin"] = settings.PriceMin;
        if (settings.PriceMax.HasValue)
            criteria["priceMax"] = settings.PriceMax;
        if (settings.MileageMax.HasValue)
            criteria["mileageMax"] = settings.MileageMax;
        if (!string.IsNullOrEmpty(settings.ZipCode))
            criteria["zipCode"] = settings.ZipCode;
        if (settings.Radius.HasValue)
            criteria["radius"] = settings.Radius;
        if (!string.IsNullOrEmpty(settings.Condition))
            criteria["condition"] = settings.Condition;
        if (!string.IsNullOrEmpty(settings.BodyStyle))
            criteria["bodyStyle"] = settings.BodyStyle;

        return JsonSerializer.Serialize(criteria, new JsonSerializerOptions { WriteIndented = true });
    }

    private string DetermineOutputPath(string? outputPath, ReportFormat format)
    {
        if (!string.IsNullOrEmpty(outputPath))
        {
            // If path has no extension, add the appropriate one
            if (string.IsNullOrEmpty(Path.GetExtension(outputPath)))
            {
                var extension = format switch
                {
                    ReportFormat.Html => ".html",
                    ReportFormat.Pdf => ".pdf",
                    ReportFormat.Excel => ".xlsx",
                    _ => ".html"
                };
                return outputPath + extension;
            }
            return outputPath;
        }

        // Default path with timestamp
        var defaultFileName = $"report_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        var fileExtension = format switch
        {
            ReportFormat.Html => ".html",
            ReportFormat.Pdf => ".pdf",
            ReportFormat.Excel => ".xlsx",
            _ => ".html"
        };

        var defaultDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".car-search",
            "reports");

        if (!Directory.Exists(defaultDir))
            Directory.CreateDirectory(defaultDir);

        return Path.Combine(defaultDir, defaultFileName + fileExtension);
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-t|--tenant-id <GUID>")]
        [Description("Tenant ID for multi-tenancy support")]
        public Guid TenantId { get; set; }

        [CommandOption("-f|--format <FORMAT>")]
        [Description("Report format (html, pdf, excel)")]
        [DefaultValue("html")]
        public string Format { get; set; } = "html";

        [CommandOption("-o|--output <PATH>")]
        [Description("Output file path")]
        public string? OutputPath { get; set; }

        [CommandOption("-n|--name <NAME>")]
        [Description("Report name")]
        public string? Name { get; set; }

        [CommandOption("--title <TITLE>")]
        [Description("Report title")]
        public string? Title { get; set; }

        [CommandOption("-m|--make <MAKE>")]
        [Description("Filter by vehicle make")]
        public string? Make { get; set; }

        [CommandOption("--model <MODEL>")]
        [Description("Filter by vehicle model")]
        public string? Model { get; set; }

        [CommandOption("--year-min <YEAR>")]
        [Description("Minimum year")]
        public int? YearMin { get; set; }

        [CommandOption("--year-max <YEAR>")]
        [Description("Maximum year")]
        public int? YearMax { get; set; }

        [CommandOption("--price-min <PRICE>")]
        [Description("Minimum price")]
        public decimal? PriceMin { get; set; }

        [CommandOption("--price-max <PRICE>")]
        [Description("Maximum price")]
        public decimal? PriceMax { get; set; }

        [CommandOption("--mileage-min <MILEAGE>")]
        [Description("Minimum mileage")]
        public int? MileageMin { get; set; }

        [CommandOption("--mileage-max <MILEAGE>")]
        [Description("Maximum mileage")]
        public int? MileageMax { get; set; }

        [CommandOption("-z|--zip <ZIPCODE>")]
        [Description("ZIP code for location-based search")]
        public string? ZipCode { get; set; }

        [CommandOption("-r|--radius <KILOMETERS>")]
        [Description("Search radius in kilometers")]
        public int? Radius { get; set; }

        [CommandOption("--condition <CONDITION>")]
        [Description("Filter by condition (New, Used, CPO)")]
        public string? Condition { get; set; }

        [CommandOption("--body-style <STYLE>")]
        [Description("Filter by body style")]
        public string? BodyStyle { get; set; }

        [CommandOption("--include-statistics")]
        [Description("Include market statistics in report")]
        [DefaultValue(true)]
        public bool IncludeStatistics { get; set; } = true;
    }
}
