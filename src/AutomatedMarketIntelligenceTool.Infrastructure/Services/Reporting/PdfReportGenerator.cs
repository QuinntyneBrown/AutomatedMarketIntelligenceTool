using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Core.Services.Reporting;
using Microsoft.Extensions.Logging;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Reporting;

public class PdfReportGenerator : IReportGenerator
{
    private readonly ILogger<PdfReportGenerator> _logger;

    // Colors
    private static readonly XColor BlueMedium = XColor.FromArgb(33, 150, 243);
    private static readonly XColor BlueDark = XColor.FromArgb(25, 118, 210);
    private static readonly XColor GreenMedium = XColor.FromArgb(76, 175, 80);
    private static readonly XColor GreyLight = XColor.FromArgb(245, 245, 245);
    private static readonly XColor GreyDark = XColor.FromArgb(97, 97, 97);
    private static readonly XColor GreyBorder = XColor.FromArgb(224, 224, 224);

    // Page settings
    private const double PageWidth = 595.0;  // A4 width in points
    private const double PageHeight = 842.0; // A4 height in points
    private const double Margin = 56.7;      // 2cm in points
    private const double ContentWidth = PageWidth - (2 * Margin);

    public ReportFormat SupportedFormat => ReportFormat.Pdf;

    public PdfReportGenerator(ILogger<PdfReportGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string> GenerateReportAsync(ReportData data, string outputPath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var document = new PdfDocument();
            document.Info.Title = data.Title;
            document.Info.Author = "Automated Market Intelligence Tool";

            var page = document.AddPage();
            page.Width = PageWidth;
            page.Height = PageHeight;

            var gfx = XGraphics.FromPdfPage(page);
            var currentY = Margin;
            var pageNumber = 1;
            var totalListings = data.Listings?.Count ?? 0;

            // Fonts
            var titleFont = new XFont("Arial", 24, XFontStyleEx.Bold);
            var subtitleFont = new XFont("Arial", 10, XFontStyleEx.Regular);
            var sectionFont = new XFont("Arial", 16, XFontStyleEx.Bold);
            var bodyFont = new XFont("Arial", 11, XFontStyleEx.Regular);
            var smallFont = new XFont("Arial", 9, XFontStyleEx.Regular);
            var tableHeaderFont = new XFont("Arial", 10, XFontStyleEx.Bold);
            var tableBodyFont = new XFont("Arial", 9, XFontStyleEx.Regular);
            var statValueFont = new XFont("Arial", 14, XFontStyleEx.Bold);
            var courierFont = new XFont("Courier New", 9, XFontStyleEx.Regular);

            // Draw Header
            currentY = DrawHeader(gfx, data, currentY, titleFont, subtitleFont);

            // Draw Search Criteria Section
            if (!string.IsNullOrEmpty(data.SearchCriteria))
            {
                currentY = DrawSearchCriteria(gfx, data.SearchCriteria, currentY, sectionFont, courierFont);
            }

            // Draw Statistics Section
            if (data.Statistics != null)
            {
                currentY = DrawStatistics(gfx, data.Statistics, currentY, sectionFont, smallFont, statValueFont);
            }

            // Draw Listings Section
            currentY = DrawListingsHeader(gfx, totalListings, currentY, sectionFont);

            if (data.Listings?.Any() == true)
            {
                // Draw table header
                currentY = DrawTableHeader(gfx, currentY, tableHeaderFont);

                // Draw listing rows
                var listingsToShow = data.Listings.Take(100).ToList();
                foreach (var listing in listingsToShow)
                {
                    // Check if we need a new page
                    if (currentY + 25 > PageHeight - Margin - 30)
                    {
                        DrawFooter(gfx, pageNumber, -1); // -1 means we don't know total pages yet
                        page = document.AddPage();
                        page.Width = PageWidth;
                        page.Height = PageHeight;
                        gfx = XGraphics.FromPdfPage(page);
                        pageNumber++;
                        currentY = Margin;
                        currentY = DrawTableHeader(gfx, currentY, tableHeaderFont);
                    }

                    currentY = DrawListingRow(gfx, listing, currentY, tableBodyFont);
                }

                // Note about truncated listings
                if (totalListings > 100)
                {
                    currentY += 10;
                    gfx.DrawString(
                        $"Note: Showing first 100 of {totalListings} listings",
                        new XFont("Arial", 9, XFontStyleEx.Italic),
                        new XSolidBrush(GreyDark),
                        new XRect(Margin, currentY, ContentWidth, 15),
                        XStringFormats.TopLeft);
                }
            }
            else
            {
                // No listings message
                currentY += 20;
                gfx.DrawString(
                    "No listings to display",
                    new XFont("Arial", 12, XFontStyleEx.Italic),
                    new XSolidBrush(GreyDark),
                    new XRect(Margin, currentY, ContentWidth, 20),
                    XStringFormats.TopCenter);
            }

            // Draw footer on last page and update all page footers
            var totalPages = document.PageCount;
            for (int i = 0; i < totalPages; i++)
            {
                var pageGfx = XGraphics.FromPdfPage(document.Pages[i]);
                DrawFooter(pageGfx, i + 1, totalPages);
            }

            document.Save(outputPath);

            _logger.LogInformation("PDF report generated successfully at {Path}", outputPath);

            return Task.FromResult(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF report");
            throw;
        }
    }

    private double DrawHeader(XGraphics gfx, ReportData data, double y, XFont titleFont, XFont subtitleFont)
    {
        // Title
        gfx.DrawString(
            data.Title,
            titleFont,
            new XSolidBrush(BlueMedium),
            new XRect(Margin, y, ContentWidth, 30),
            XStringFormats.TopLeft);
        y += 35;

        // Generated date
        gfx.DrawString(
            $"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC",
            subtitleFont,
            new XSolidBrush(GreyDark),
            new XRect(Margin, y, ContentWidth, 15),
            XStringFormats.TopLeft);
        y += 20;

        // Header border
        gfx.DrawLine(new XPen(BlueMedium, 2), Margin, y, PageWidth - Margin, y);
        y += 20;

        return y;
    }

    private double DrawSearchCriteria(XGraphics gfx, string criteria, double y, XFont sectionFont, XFont courierFont)
    {
        // Section title
        gfx.DrawString(
            "Search Criteria",
            sectionFont,
            new XSolidBrush(BlueMedium),
            new XRect(Margin, y, ContentWidth, 20),
            XStringFormats.TopLeft);
        y += 25;

        // Criteria box background
        var boxHeight = 40.0;
        gfx.DrawRectangle(
            new XSolidBrush(GreyLight),
            Margin + 10, y, ContentWidth - 20, boxHeight);

        // Criteria text
        gfx.DrawString(
            criteria,
            courierFont,
            XBrushes.Black,
            new XRect(Margin + 20, y + 10, ContentWidth - 40, boxHeight - 20),
            XStringFormats.TopLeft);

        y += boxHeight + 15;
        return y;
    }

    private double DrawStatistics(XGraphics gfx, MarketStatistics stats, double y, XFont sectionFont, XFont smallFont, XFont valueFont)
    {
        // Section title
        gfx.DrawString(
            "Market Statistics",
            sectionFont,
            new XSolidBrush(BlueMedium),
            new XRect(Margin, y, ContentWidth, 20),
            XStringFormats.TopLeft);
        y += 30;

        var statCards = new List<(string label, string value)>
        {
            ("Total Listings", stats.TotalListings.ToString("N0"))
        };

        if (stats.AveragePrice.HasValue)
            statCards.Add(("Average Price", $"${stats.AveragePrice.Value:N2}"));
        if (stats.MedianPrice.HasValue)
            statCards.Add(("Median Price", $"${stats.MedianPrice.Value:N2}"));
        if (stats.AverageMileage.HasValue)
            statCards.Add(("Avg Mileage", $"{stats.AverageMileage.Value:N0} mi"));

        var cardWidth = (ContentWidth - (statCards.Count - 1) * 10) / statCards.Count;
        var cardHeight = 50.0;

        for (int i = 0; i < statCards.Count; i++)
        {
            var x = Margin + i * (cardWidth + 10);

            // Card background
            gfx.DrawRectangle(new XSolidBrush(GreyLight), x, y, cardWidth, cardHeight);

            // Label
            gfx.DrawString(
                statCards[i].label,
                smallFont,
                new XSolidBrush(GreyDark),
                new XRect(x + 5, y + 8, cardWidth - 10, 15),
                XStringFormats.TopLeft);

            // Value
            gfx.DrawString(
                statCards[i].value,
                valueFont,
                new XSolidBrush(BlueDark),
                new XRect(x + 5, y + 25, cardWidth - 10, 20),
                XStringFormats.TopLeft);
        }

        y += cardHeight + 20;
        return y;
    }

    private double DrawListingsHeader(XGraphics gfx, int totalListings, double y, XFont sectionFont)
    {
        gfx.DrawString(
            $"Listings ({totalListings})",
            sectionFont,
            new XSolidBrush(BlueMedium),
            new XRect(Margin, y, ContentWidth, 20),
            XStringFormats.TopLeft);
        y += 30;
        return y;
    }

    private double DrawTableHeader(XGraphics gfx, double y, XFont font)
    {
        var columns = GetColumnWidths();
        var headerHeight = 22.0;
        var x = Margin;

        string[] headers = { "Year", "Make", "Model", "Price", "Mileage", "Location", "Source" };

        for (int i = 0; i < headers.Length; i++)
        {
            gfx.DrawRectangle(new XSolidBrush(BlueMedium), x, y, columns[i], headerHeight);
            gfx.DrawString(
                headers[i],
                font,
                XBrushes.White,
                new XRect(x + 5, y + 4, columns[i] - 10, headerHeight - 8),
                XStringFormats.TopLeft);
            x += columns[i];
        }

        return y + headerHeight;
    }

    private double DrawListingRow(XGraphics gfx, Core.Models.ListingAggregate.Listing listing, double y, XFont font)
    {
        var columns = GetColumnWidths();
        var rowHeight = 20.0;
        var x = Margin;

        string[] values =
        {
            listing.Year.ToString(),
            listing.Make ?? "N/A",
            listing.Model ?? "N/A",
            $"${listing.Price:N2}",
            listing.Mileage.HasValue ? $"{listing.Mileage.Value:N0}" : "N/A",
            listing.Location ?? "N/A",
            listing.SourceSite ?? "N/A"
        };

        XBrush[] brushes =
        {
            XBrushes.Black,
            XBrushes.Black,
            XBrushes.Black,
            new XSolidBrush(GreenMedium),
            XBrushes.Black,
            XBrushes.Black,
            XBrushes.Black
        };

        for (int i = 0; i < values.Length; i++)
        {
            // Cell border bottom
            gfx.DrawLine(new XPen(GreyBorder, 1), x, y + rowHeight, x + columns[i], y + rowHeight);

            // Truncate text if too long
            var displayValue = TruncateText(values[i], font, columns[i] - 10, gfx);

            gfx.DrawString(
                displayValue,
                font,
                brushes[i],
                new XRect(x + 5, y + 3, columns[i] - 10, rowHeight - 6),
                XStringFormats.TopLeft);
            x += columns[i];
        }

        return y + rowHeight;
    }

    private double[] GetColumnWidths()
    {
        // Proportional column widths that sum to ContentWidth
        var totalParts = 40 + 75 + 100 + 60 + 60 + 85 + 62;
        var scale = ContentWidth / totalParts;
        return new[]
        {
            40 * scale,   // Year
            75 * scale,   // Make
            100 * scale,  // Model
            60 * scale,   // Price
            60 * scale,   // Mileage
            85 * scale,   // Location
            62 * scale    // Source
        };
    }

    private string TruncateText(string text, XFont font, double maxWidth, XGraphics gfx)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var size = gfx.MeasureString(text, font);
        if (size.Width <= maxWidth) return text;

        // Truncate with ellipsis
        for (int len = text.Length - 1; len > 0; len--)
        {
            var truncated = text.Substring(0, len) + "...";
            size = gfx.MeasureString(truncated, font);
            if (size.Width <= maxWidth) return truncated;
        }

        return "...";
    }

    private void DrawFooter(XGraphics gfx, int pageNumber, int totalPages)
    {
        var footerY = PageHeight - Margin + 10;
        var footerFont = new XFont("Arial", 10, XFontStyleEx.Regular);

        var footerText = totalPages > 0
            ? $"Page {pageNumber} of {totalPages}"
            : $"Page {pageNumber}";

        gfx.DrawString(
            footerText,
            footerFont,
            XBrushes.Black,
            new XRect(Margin, footerY, ContentWidth, 15),
            XStringFormats.TopCenter);
    }
}
