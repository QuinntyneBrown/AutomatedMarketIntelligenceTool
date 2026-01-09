using System.Collections.Concurrent;
using System.Diagnostics;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scraping;

/// <summary>
/// Default implementation of concurrent scraping engine.
/// </summary>
public class ConcurrentScrapingEngine : IConcurrentScrapingEngine
{
    private readonly IResourceManager _resourceManager;
    private readonly ILogger<ConcurrentScrapingEngine> _logger;

    public ConcurrentScrapingEngine(
        IResourceManager resourceManager,
        ILogger<ConcurrentScrapingEngine> logger)
    {
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ConcurrentScrapeResult>> ScrapeAsync(
        IEnumerable<ISiteScraper> scrapers,
        SearchParameters parameters,
        int concurrencyLevel = 3,
        IProgress<ConcurrentScrapeProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var scraperList = scrapers.ToList();
        
        if (!scraperList.Any())
        {
            _logger.LogWarning("No scrapers provided for concurrent scraping");
            return Array.Empty<ConcurrentScrapeResult>();
        }

        // Validate and adjust concurrency level
        var adjustedConcurrency = _resourceManager.GetRecommendedConcurrency(
            Math.Max(1, Math.Min(concurrencyLevel, scraperList.Count)));

        _logger.LogInformation(
            "Starting concurrent scrape with {ScraperCount} scrapers, concurrency level: {Concurrency}",
            scraperList.Count,
            adjustedConcurrency);

        var results = new ConcurrentBag<ConcurrentScrapeResult>();
        var completedCount = 0;
        var inProgressCount = 0;

        // Use Parallel.ForEachAsync for concurrent execution
        await Parallel.ForEachAsync(
            scraperList,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = adjustedConcurrency,
                CancellationToken = cancellationToken
            },
            async (scraper, ct) =>
            {
                var stopwatch = Stopwatch.StartNew();
                
                try
                {
                    // Track in-progress count
                    Interlocked.Increment(ref inProgressCount);

                    // Report start
                    progress?.Report(new ConcurrentScrapeProgress
                    {
                        SiteName = scraper.SiteName,
                        TotalSites = scraperList.Count,
                        CompletedSites = completedCount,
                        InProgressSites = inProgressCount,
                        EventType = ConcurrentScrapeEventType.Started,
                        Message = $"Starting scrape for {scraper.SiteName}"
                    });

                    _logger.LogInformation("Starting scrape for site: {SiteName}", scraper.SiteName);

                    // Execute the scrape
                    var listings = await scraper.ScrapeAsync(parameters, null, ct);
                    var listingsList = listings.ToList();

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "Completed scrape for {SiteName}: {Count} listings in {Elapsed}",
                        scraper.SiteName,
                        listingsList.Count,
                        stopwatch.Elapsed);

                    // Add successful result
                    results.Add(new ConcurrentScrapeResult
                    {
                        SiteName = scraper.SiteName,
                        Listings = listingsList,
                        Success = true,
                        ElapsedTime = stopwatch.Elapsed
                    });

                    // Update counts and report completion
                    Interlocked.Decrement(ref inProgressCount);
                    Interlocked.Increment(ref completedCount);

                    progress?.Report(new ConcurrentScrapeProgress
                    {
                        SiteName = scraper.SiteName,
                        TotalSites = scraperList.Count,
                        CompletedSites = completedCount,
                        InProgressSites = inProgressCount,
                        EventType = ConcurrentScrapeEventType.Completed,
                        Message = $"Completed {scraper.SiteName}: {listingsList.Count} listings"
                    });
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        ex,
                        "Error scraping {SiteName}: {ErrorMessage}",
                        scraper.SiteName,
                        ex.Message);

                    // Add failed result
                    results.Add(new ConcurrentScrapeResult
                    {
                        SiteName = scraper.SiteName,
                        Listings = Array.Empty<ScrapedListing>(),
                        Success = false,
                        ErrorMessage = ex.Message,
                        Exception = ex,
                        ElapsedTime = stopwatch.Elapsed
                    });

                    // Update counts and report failure
                    Interlocked.Decrement(ref inProgressCount);
                    Interlocked.Increment(ref completedCount);

                    progress?.Report(new ConcurrentScrapeProgress
                    {
                        SiteName = scraper.SiteName,
                        TotalSites = scraperList.Count,
                        CompletedSites = completedCount,
                        InProgressSites = inProgressCount,
                        EventType = ConcurrentScrapeEventType.Failed,
                        Message = $"Failed {scraper.SiteName}: {ex.Message}"
                    });
                }
            });

        _logger.LogInformation(
            "Concurrent scrape completed: {Successful} successful, {Failed} failed",
            results.Count(r => r.Success),
            results.Count(r => !r.Success));

        return results.OrderBy(r => r.SiteName).ToList();
    }
}
