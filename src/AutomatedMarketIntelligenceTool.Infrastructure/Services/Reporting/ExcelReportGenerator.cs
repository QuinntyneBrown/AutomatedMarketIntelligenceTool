using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Reporting;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Reporting;

public class ExcelReportGenerator : IReportGenerator
{
    private readonly ILogger<ExcelReportGenerator> _logger;

    public ReportFormat SupportedFormat => ReportFormat.Excel;

    public ExcelReportGenerator(ILogger<ExcelReportGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string> GenerateReportAsync(ReportData data, string outputPath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var workbook = new XLWorkbook();

            // Create Summary Sheet
            CreateSummarySheet(workbook, data);

            // Create Listings Sheet
            CreateListingsSheet(workbook, data);

            // Create Statistics Sheet (if data available)
            if (data.Statistics != null)
            {
                CreateStatisticsSheet(workbook, data);
            }

            workbook.SaveAs(outputPath);
            
            _logger.LogInformation("Excel report generated successfully at {Path}", outputPath);
            
            return Task.FromResult(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Excel report");
            throw;
        }
    }

    private void CreateSummarySheet(XLWorkbook workbook, ReportData data)
    {
        var ws = workbook.Worksheets.Add("Summary");

        // Title
        ws.Cell(1, 1).Value = data.Title;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0066CC");

        // Generated date
        ws.Cell(2, 1).Value = "Generated:";
        ws.Cell(2, 2).Value = data.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss UTC");
        ws.Cell(2, 1).Style.Font.Bold = true;

        // Search Criteria
        if (!string.IsNullOrEmpty(data.SearchCriteria))
        {
            ws.Cell(4, 1).Value = "Search Criteria:";
            ws.Cell(4, 1).Style.Font.Bold = true;
            ws.Cell(5, 1).Value = data.SearchCriteria;
            ws.Cell(5, 1).Style.Alignment.WrapText = true;
        }

        // Statistics Summary
        if (data.Statistics != null)
        {
            int row = 7;
            ws.Cell(row, 1).Value = "Key Statistics:";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            row++;

            ws.Cell(row, 1).Value = "Total Listings:";
            ws.Cell(row, 2).Value = data.Statistics.TotalListings;
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            if (data.Statistics.AveragePrice.HasValue)
            {
                ws.Cell(row, 1).Value = "Average Price:";
                ws.Cell(row, 2).Value = data.Statistics.AveragePrice.Value;
                ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 1).Style.Font.Bold = true;
                row++;
            }

            if (data.Statistics.MedianPrice.HasValue)
            {
                ws.Cell(row, 1).Value = "Median Price:";
                ws.Cell(row, 2).Value = data.Statistics.MedianPrice.Value;
                ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 1).Style.Font.Bold = true;
                row++;
            }

            if (data.Statistics.AverageMileage.HasValue)
            {
                ws.Cell(row, 1).Value = "Average Mileage:";
                ws.Cell(row, 2).Value = data.Statistics.AverageMileage.Value;
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 1).Style.Font.Bold = true;
            }
        }

        // Auto-fit columns
        ws.Columns().AdjustToContents();
    }

    private void CreateListingsSheet(XLWorkbook workbook, ReportData data)
    {
        var ws = workbook.Worksheets.Add("Listings");

        // Headers
        var headers = new[] { "Year", "Make", "Model", "Trim", "Price", "Mileage", "VIN", 
                              "Location", "Dealer", "Source", "Condition", "Body Style", "URL" };
        
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0066CC");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data rows
        if (data.Listings?.Any() == true)
        {
            int row = 2;
            foreach (var listing in data.Listings)
            {
                ws.Cell(row, 1).Value = listing.Year;
                ws.Cell(row, 2).Value = listing.Make;
                ws.Cell(row, 3).Value = listing.Model;
                ws.Cell(row, 4).Value = listing.Trim;
                
                ws.Cell(row, 5).Value = listing.Price;
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                
                ws.Cell(row, 6).Value = listing.Mileage;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                
                ws.Cell(row, 7).Value = listing.Vin;
                ws.Cell(row, 8).Value = listing.Location;
                ws.Cell(row, 9).Value = listing.Dealer;
                ws.Cell(row, 10).Value = listing.Source;
                ws.Cell(row, 11).Value = listing.Condition;
                ws.Cell(row, 12).Value = listing.BodyStyle;
                ws.Cell(row, 13).Value = listing.Url;

                // Alternate row colors
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F0F0");
                }

                row++;
            }

            // Create table
            var dataRange = ws.Range(1, 1, row - 1, headers.Length);
            var table = dataRange.CreateTable();
            table.Theme = XLTableTheme.TableStyleMedium2;
        }

        // Auto-fit columns
        ws.Columns().AdjustToContents();
    }

    private void CreateStatisticsSheet(XLWorkbook workbook, ReportData data)
    {
        var ws = workbook.Worksheets.Add("Statistics");
        var stats = data.Statistics!;

        int row = 1;

        // Overview Statistics
        ws.Cell(row, 1).Value = "Overview Statistics";
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Font.Bold = true;
        row += 2;

        AddStatRow(ws, ref row, "Total Listings", stats.TotalListings);
        AddStatRow(ws, ref row, "Average Price", stats.AveragePrice, "$#,##0.00");
        AddStatRow(ws, ref row, "Median Price", stats.MedianPrice, "$#,##0.00");
        AddStatRow(ws, ref row, "Min Price", stats.MinPrice, "$#,##0.00");
        AddStatRow(ws, ref row, "Max Price", stats.MaxPrice, "$#,##0.00");
        AddStatRow(ws, ref row, "Average Mileage", stats.AverageMileage, "#,##0");
        AddStatRow(ws, ref row, "Median Mileage", stats.MedianMileage, "#,##0");
        AddStatRow(ws, ref row, "Min Mileage", stats.MinMileage, "#,##0");
        AddStatRow(ws, ref row, "Max Mileage", stats.MaxMileage, "#,##0");
        AddStatRow(ws, ref row, "Avg Days on Market", stats.AverageDaysOnMarket, "#,##0.0");

        // Count by Make
        if (stats.CountByMake?.Any() == true)
        {
            row += 2;
            ws.Cell(row, 1).Value = "Count by Make";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;
            
            ws.Cell(row, 1).Value = "Make";
            ws.Cell(row, 2).Value = "Count";
            ws.Range(row, 1, row, 2).Style.Font.Bold = true;
            ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#0066CC");
            ws.Range(row, 1, row, 2).Style.Font.FontColor = XLColor.White;
            row++;

            foreach (var item in stats.CountByMake.OrderByDescending(x => x.Value))
            {
                ws.Cell(row, 1).Value = item.Key;
                ws.Cell(row, 2).Value = item.Value;
                row++;
            }
        }

        // Count by Year
        if (stats.CountByYear?.Any() == true)
        {
            row += 2;
            ws.Cell(row, 4).Value = "Count by Year";
            ws.Cell(row, 4).Style.Font.Bold = true;
            row++;
            
            ws.Cell(row, 4).Value = "Year";
            ws.Cell(row, 5).Value = "Count";
            ws.Range(row, 4, row, 5).Style.Font.Bold = true;
            ws.Range(row, 4, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#0066CC");
            ws.Range(row, 4, row, 5).Style.Font.FontColor = XLColor.White;
            row++;

            foreach (var item in stats.CountByYear.OrderByDescending(x => x.Key))
            {
                ws.Cell(row, 4).Value = item.Key;
                ws.Cell(row, 5).Value = item.Value;
                row++;
            }
        }

        // Auto-fit columns
        ws.Columns().AdjustToContents();
    }

    private void AddStatRow(IXLWorksheet ws, ref int row, string label, object? value, string? format = null)
    {
        if (value == null)
            return;

        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = value;
        
        if (!string.IsNullOrEmpty(format))
        {
            ws.Cell(row, 2).Style.NumberFormat.Format = format;
        }
        
        row++;
    }
}
