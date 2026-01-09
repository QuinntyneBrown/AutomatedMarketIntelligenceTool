using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Reporting;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Reporting;

public class PdfReportGenerator : IReportGenerator
{
    private readonly ILogger<PdfReportGenerator> _logger;

    public ReportFormat SupportedFormat => ReportFormat.Pdf;

    public PdfReportGenerator(ILogger<PdfReportGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Set QuestPDF license (Community license for open source projects)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<string> GenerateReportAsync(ReportData data, string outputPath, CancellationToken cancellationToken = default)
    {
        try
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .BorderBottom(2)
                        .BorderColor(Colors.Blue.Medium)
                        .PaddingBottom(10)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text(data.Title)
                                    .FontSize(24)
                                    .SemiBold()
                                    .FontColor(Colors.Blue.Medium);
                                
                                column.Item().Text($"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken2);
                            });
                        });

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            // Search Criteria Section
                            if (!string.IsNullOrEmpty(data.SearchCriteria))
                            {
                                column.Item().PaddingBottom(15).Column(c =>
                                {
                                    c.Item().Text("Search Criteria")
                                        .FontSize(16)
                                        .SemiBold()
                                        .FontColor(Colors.Blue.Medium);
                                    
                                    c.Item().PaddingTop(5).PaddingLeft(10)
                                        .Background(Colors.Grey.Lighten3)
                                        .Padding(10)
                                        .Text(data.SearchCriteria)
                                        .FontSize(9)
                                        .FontFamily(Fonts.Courier);
                                });
                            }

                            // Statistics Section
                            if (data.Statistics != null)
                            {
                                column.Item().PaddingBottom(15).Column(c =>
                                {
                                    c.Item().Text("Market Statistics")
                                        .FontSize(16)
                                        .SemiBold()
                                        .FontColor(Colors.Blue.Medium);
                                    
                                    c.Item().PaddingTop(10).Row(row =>
                                    {
                                        CreateStatCard(row, "Total Listings", data.Statistics.TotalListings.ToString("N0"));
                                        
                                        if (data.Statistics.AveragePrice.HasValue)
                                            CreateStatCard(row, "Average Price", $"${data.Statistics.AveragePrice.Value:N2}");
                                        
                                        if (data.Statistics.MedianPrice.HasValue)
                                            CreateStatCard(row, "Median Price", $"${data.Statistics.MedianPrice.Value:N2}");
                                        
                                        if (data.Statistics.AverageMileage.HasValue)
                                            CreateStatCard(row, "Avg Mileage", $"{data.Statistics.AverageMileage.Value:N0} mi");
                                    });
                                });
                            }

                            // Listings Table
                            column.Item().Column(c =>
                            {
                                c.Item().Text($"Listings ({data.Listings?.Count ?? 0})")
                                    .FontSize(16)
                                    .SemiBold()
                                    .FontColor(Colors.Blue.Medium);
                                
                                if (data.Listings?.Any() == true)
                                {
                                    c.Item().PaddingTop(10).Table(table =>
                                    {
                                        // Define columns
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(40);  // Year
                                            columns.RelativeColumn(1.5f); // Make
                                            columns.RelativeColumn(2);    // Model
                                            columns.RelativeColumn(1);    // Price
                                            columns.RelativeColumn(1);    // Mileage
                                            columns.RelativeColumn(1.5f); // Location
                                            columns.RelativeColumn(1);    // Source
                                        });

                                        // Header
                                        table.Header(header =>
                                        {
                                            header.Cell().Background(Colors.Blue.Medium)
                                                .Padding(5).Text("Year").FontColor(Colors.White).SemiBold();
                                            header.Cell().Background(Colors.Blue.Medium)
                                                .Padding(5).Text("Make").FontColor(Colors.White).SemiBold();
                                            header.Cell().Background(Colors.Blue.Medium)
                                                .Padding(5).Text("Model").FontColor(Colors.White).SemiBold();
                                            header.Cell().Background(Colors.Blue.Medium)
                                                .Padding(5).Text("Price").FontColor(Colors.White).SemiBold();
                                            header.Cell().Background(Colors.Blue.Medium)
                                                .Padding(5).Text("Mileage").FontColor(Colors.White).SemiBold();
                                            header.Cell().Background(Colors.Blue.Medium)
                                                .Padding(5).Text("Location").FontColor(Colors.White).SemiBold();
                                            header.Cell().Background(Colors.Blue.Medium)
                                                .Padding(5).Text("Source").FontColor(Colors.White).SemiBold();
                                        });

                                        // Rows
                                        foreach (var listing in data.Listings.Take(100)) // Limit to first 100 for PDF
                                        {
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                                .Padding(5).Text(listing.Year.ToString());
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                                .Padding(5).Text(listing.Make ?? "N/A");
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                                .Padding(5).Text(listing.Model ?? "N/A");
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                                .Padding(5).Text($"${listing.Price:N2}").FontColor(Colors.Green.Medium).SemiBold();
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                                .Padding(5).Text(listing.Mileage.HasValue ? $"{listing.Mileage.Value:N0}" : "N/A");
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                                .Padding(5).Text(listing.Location ?? "N/A");
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                                .Padding(5).Text(listing.SourceSite ?? "N/A");
                                        }
                                    });

                                    if (data.Listings.Count > 100)
                                    {
                                        c.Item().PaddingTop(10)
                                            .Text($"Note: Showing first 100 of {data.Listings.Count} listings")
                                            .FontSize(9)
                                            .Italic()
                                            .FontColor(Colors.Grey.Darken1);
                                    }
                                }
                                else
                                {
                                    c.Item().PaddingTop(20).AlignCenter()
                                        .Text("No listings to display")
                                        .FontSize(12)
                                        .Italic()
                                        .FontColor(Colors.Grey.Darken1);
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            })
            .GeneratePdf(outputPath);

            _logger.LogInformation("PDF report generated successfully at {Path}", outputPath);
            
            return Task.FromResult(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF report");
            throw;
        }
    }

    private void CreateStatCard(RowDescriptor row, string label, string value)
    {
        row.RelativeItem().Padding(5).Background(Colors.Grey.Lighten3).Column(column =>
        {
            column.Item().Text(label)
                .FontSize(9)
                .FontColor(Colors.Grey.Darken2);
            column.Item().PaddingTop(3).Text(value)
                .FontSize(14)
                .SemiBold()
                .FontColor(Colors.Blue.Darken2);
        });
    }
}
