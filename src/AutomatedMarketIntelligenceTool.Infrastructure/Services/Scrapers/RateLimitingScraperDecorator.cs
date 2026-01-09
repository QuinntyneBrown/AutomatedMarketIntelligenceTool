using AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

/// <summary>
/// Decorator that adds rate limiting to any ISiteScraper implementation.
/// </summary>
public class RateLimitingScraperDecorator : ISiteScraper
{
    private readonly ISiteScraper _innerScraper;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitingScraperDecorator> _logger;

    public RateLimitingScraperDecorator(
        ISiteScraper innerScraper,
        IRateLimiter rateLimiter,
        ILogger<RateLimitingScraperDecorator> logger)
    {
        _innerScraper = innerScraper ?? throw new ArgumentNullException(nameof(innerScraper));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string SiteName => _innerScraper.SiteName;

    public async Task<IEnumerable<ScrapedListing>> ScrapeAsync(
        SearchParameters parameters,
        IProgress<ScrapeProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var domain = GetDomain(SiteName);

        try
        {
            // Wait for rate limiter before starting scrape
            _logger.LogDebug("Applying rate limiting for domain: {Domain}", domain);
            await _rateLimiter.WaitAsync(domain, cancellationToken);

            // Execute the actual scrape
            var result = await _innerScraper.ScrapeAsync(parameters, progress, cancellationToken);

            _logger.LogDebug("Scrape completed successfully for {SiteName}", SiteName);
            return result;
        }
        catch (Exception ex) when (IsRateLimitException(ex))
        {
            // Report rate limit hit to increase backoff
            _logger.LogWarning(
                "Rate limit detected for {SiteName}, reporting to rate limiter",
                SiteName);

            _rateLimiter.ReportRateLimitHit(domain);

            // Wait with increased backoff and retry once
            await _rateLimiter.WaitAsync(domain, cancellationToken);

            _logger.LogInformation("Retrying scrape after rate limit backoff for {SiteName}", SiteName);
            return await _innerScraper.ScrapeAsync(parameters, progress, cancellationToken);
        }
    }

    private static string GetDomain(string siteName)
    {
        // Extract domain from site name
        // e.g., "Autotrader.ca" -> "autotrader.ca"
        return siteName.ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("www.", string.Empty);
    }

    private static bool IsRateLimitException(Exception ex)
    {
        // Check if the exception message indicates a rate limit
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("429") ||
               message.Contains("too many requests") ||
               message.Contains("rate limit") ||
               (ex.InnerException != null && IsRateLimitException(ex.InnerException));
    }
}
