using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Reporting;
using Microsoft.Extensions.Logging;
using Scriban;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Reporting;

public class HtmlReportGenerator : IReportGenerator
{
    private readonly ILogger<HtmlReportGenerator> _logger;

    public ReportFormat SupportedFormat => ReportFormat.Html;

    public HtmlReportGenerator(ILogger<HtmlReportGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GenerateReportAsync(ReportData data, string outputPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = GetHtmlTemplate();
            var compiledTemplate = Template.Parse(template);
            
            var html = await compiledTemplate.RenderAsync(new
            {
                title = data.Title,
                generated_at = data.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                search_criteria = data.SearchCriteria,
                statistics = data.Statistics,
                listings = data.Listings,
                has_statistics = data.Statistics != null,
                has_listings = data.Listings?.Any() ?? false,
                listing_count = data.Listings?.Count ?? 0
            });

            await File.WriteAllTextAsync(outputPath, html, cancellationToken);
            
            _logger.LogInformation("HTML report generated successfully at {Path}", outputPath);
            
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate HTML report");
            throw;
        }
    }

    private string GetHtmlTemplate()
    {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{{ title }}</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            line-height: 1.6;
            color: #333;
            background: #f5f5f5;
            padding: 20px;
        }
        
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        
        header {
            border-bottom: 3px solid #007bff;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }
        
        h1 {
            color: #007bff;
            font-size: 2em;
            margin-bottom: 10px;
        }
        
        .meta {
            color: #666;
            font-size: 0.9em;
        }
        
        section {
            margin-bottom: 40px;
        }
        
        h2 {
            color: #333;
            font-size: 1.5em;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 2px solid #e0e0e0;
        }
        
        .criteria {
            background: #f8f9fa;
            padding: 15px;
            border-radius: 4px;
            border-left: 4px solid #007bff;
        }
        
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }
        
        .stat-card {
            background: #f8f9fa;
            padding: 20px;
            border-radius: 4px;
            border-left: 4px solid #28a745;
        }
        
        .stat-label {
            color: #666;
            font-size: 0.9em;
            margin-bottom: 5px;
        }
        
        .stat-value {
            font-size: 1.8em;
            font-weight: bold;
            color: #333;
        }
        
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }
        
        th, td {
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #e0e0e0;
        }
        
        th {
            background: #007bff;
            color: white;
            font-weight: 600;
        }
        
        tr:hover {
            background: #f8f9fa;
        }
        
        .price {
            color: #28a745;
            font-weight: 600;
        }
        
        .no-data {
            text-align: center;
            padding: 40px;
            color: #999;
            font-style: italic;
        }
        
        @media print {
            body {
                background: white;
                padding: 0;
            }
            
            .container {
                box-shadow: none;
            }
        }
        
        @media (max-width: 768px) {
            .stats-grid {
                grid-template-columns: 1fr;
            }
            
            table {
                font-size: 0.9em;
            }
            
            th, td {
                padding: 8px;
            }
        }
    </style>
</head>
<body>
    <div class=""container"">
        <header>
            <h1>{{ title }}</h1>
            <p class=""meta"">Generated: {{ generated_at }}</p>
        </header>
        
        {{if search_criteria}}
        <section>
            <h2>Search Criteria</h2>
            <div class=""criteria"">
                <pre>{{ search_criteria }}</pre>
            </div>
        </section>
        {{end}}
        
        {{if has_statistics}}
        <section>
            <h2>Market Statistics</h2>
            <div class=""stats-grid"">
                <div class=""stat-card"">
                    <div class=""stat-label"">Total Listings</div>
                    <div class=""stat-value"">{{ statistics.total_listings }}</div>
                </div>
                {{if statistics.average_price}}
                <div class=""stat-card"">
                    <div class=""stat-label"">Average Price</div>
                    <div class=""stat-value"">${{ statistics.average_price | format '%0.2f' }}</div>
                </div>
                {{end}}
                {{if statistics.median_price}}
                <div class=""stat-card"">
                    <div class=""stat-label"">Median Price</div>
                    <div class=""stat-value"">${{ statistics.median_price | format '%0.2f' }}</div>
                </div>
                {{end}}
                {{if statistics.average_mileage}}
                <div class=""stat-card"">
                    <div class=""stat-label"">Average Mileage</div>
                    <div class=""stat-value"">{{ statistics.average_mileage | format '%0.0f' }} mi</div>
                </div>
                {{end}}
            </div>
        </section>
        {{end}}
        
        <section>
            <h2>Listings ({{ listing_count }})</h2>
            {{if has_listings}}
            <table>
                <thead>
                    <tr>
                        <th>Year</th>
                        <th>Make</th>
                        <th>Model</th>
                        <th>Price</th>
                        <th>Mileage</th>
                        <th>Location</th>
                        <th>Source</th>
                    </tr>
                </thead>
                <tbody>
                    {{for listing in listings}}
                    <tr>
                        <td>{{ listing.year }}</td>
                        <td>{{ listing.make }}</td>
                        <td>{{ listing.model }}</td>
                        <td class=""price"">${{ listing.price | format '%0.2f' }}</td>
                        <td>{{ listing.mileage | format '%0.0f' }} mi</td>
                        <td>{{ listing.location }}</td>
                        <td>{{ listing.source }}</td>
                    </tr>
                    {{end}}
                </tbody>
            </table>
            {{else}}
            <div class=""no-data"">No listings to display</div>
            {{end}}
        </section>
    </div>
</body>
</html>";
    }
}
