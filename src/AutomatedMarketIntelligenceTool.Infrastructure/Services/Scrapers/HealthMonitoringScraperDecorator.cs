using System.Diagnostics;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

/// <summary>
/// Decorator that adds health monitoring to any ISiteScraper implementation.
/// </summary>
public class HealthMonitoringScraperDecorator : ISiteScraper
{
    private readonly ISiteScraper _innerScraper;
    private readonly IScraperHealthService _healthService;
    private readonly ILogger<HealthMonitoringScraperDecorator> _logger;

    public HealthMonitoringScraperDecorator(
        ISiteScraper innerScraper,
        IScraperHealthService healthService,
        ILogger<HealthMonitoringScraperDecorator> logger)
    {
        _innerScraper = innerScraper ?? throw new ArgumentNullException(nameof(innerScraper));
        _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string SiteName => _innerScraper.SiteName;

    public async Task<IEnumerable<ScrapedListing>> ScrapeAsync(
        SearchParameters parameters,
        IProgress<ScrapeProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        int listingsCount = 0;
        string? errorMessage = null;

        try
        {
            _logger.LogDebug("Starting scrape with health monitoring for {SiteName}", SiteName);

            // Execute the actual scrape
            var result = await _innerScraper.ScrapeAsync(parameters, progress, cancellationToken);
            
            var listings = result.ToList();
            listingsCount = listings.Count;
            success = true;

            _logger.LogDebug(
                "Scrape completed successfully for {SiteName}: {Count} listings found in {ElapsedMs}ms",
                SiteName, listingsCount, stopwatch.ElapsedMilliseconds);

            return listings;
        }
        catch (Exception ex)
        {
            success = false;
            errorMessage = $"{ex.GetType().Name}: {ex.Message}";
            
            _logger.LogError(
                ex,
                "Scrape failed for {SiteName} after {ElapsedMs}ms",
                SiteName, stopwatch.ElapsedMilliseconds);

            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Record the attempt with health service
            _healthService.RecordAttempt(
                SiteName,
                success,
                stopwatch.ElapsedMilliseconds,
                listingsCount,
                errorMessage);
        }
    }
}
