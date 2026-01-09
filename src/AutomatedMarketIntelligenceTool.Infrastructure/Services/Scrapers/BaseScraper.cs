using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public abstract class BaseScraper : ISiteScraper
{
    protected readonly ILogger _logger;
    protected const int MaxRetries = 3;
    protected const int MaxPages = 50;
    protected const int RateLimitDelayMs = 2000;
    protected const int RetryDelayMs = 1000;

    public abstract string SiteName { get; }

    protected BaseScraper(ILogger logger)
    {
        _logger = logger;
    }

    protected abstract string BuildSearchUrl(SearchParameters parameters, int page);

    protected abstract Task<IEnumerable<ScrapedListing>> ParseListingsAsync(IPage page, CancellationToken cancellationToken);

    protected abstract Task<bool> HasNextPageAsync(IPage page, CancellationToken cancellationToken);

    public async Task<IEnumerable<ScrapedListing>> ScrapeAsync(
        SearchParameters parameters,
        IProgress<ScrapeProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var allListings = new List<ScrapedListing>();
        var playwright = await Playwright.CreateAsync();
        IBrowser? browser = null;
        IBrowserContext? context = null;

        try
        {
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = !parameters.HeadedMode,
                Args = new[] { "--disable-blink-features=AutomationControlled" }
            });

            context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                // A more "realistic" profile helps reduce bot-blocking.
                Locale = "en-CA",
                TimezoneId = "America/Toronto",
                ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                    "AppleWebKit/537.36 (KHTML, like Gecko) " +
                    "Chrome/120.0.0.0 Safari/537.36",
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept-Language"] = "en-CA,en;q=0.9"
                }
            });

            await context.AddInitScriptAsync(
                "Object.defineProperty(navigator, 'webdriver', { get: () => undefined });");

            var maxPages = parameters.MaxPages ?? MaxPages;
            var currentPage = 1;

            while (currentPage <= maxPages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "Scraping {SiteName} page {CurrentPage} (max: {MaxPages})",
                    SiteName,
                    currentPage,
                    maxPages);

                var pageListings = await ScrapePageWithRetryAsync(
                    context,
                    parameters,
                    currentPage,
                    cancellationToken);

                var hasNext = pageListings.HasNext;
                var listings = pageListings.Listings;

                if (listings == null || listings.Count == 0)
                {
                    _logger.LogInformation(
                        "No listings found on page {CurrentPage}, ending pagination",
                        currentPage);
                    break;
                }

                allListings.AddRange(listings);

                progress?.Report(new ScrapeProgress
                {
                    SiteName = SiteName,
                    CurrentPage = currentPage,
                    TotalListingsFound = allListings.Count,
                    Message = $"Found {listings.Count} listings on page {currentPage}"
                });

                if (!hasNext)
                {
                    _logger.LogInformation("No more pages available, ending pagination");
                    break;
                }

                currentPage++;

                await Task.Delay(RateLimitDelayMs, cancellationToken);
            }

            _logger.LogInformation(
                "Completed scraping {SiteName}. Found {TotalListings} listings across {PageCount} pages",
                SiteName,
                allListings.Count,
                currentPage);

            return allListings;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Scraping operation was cancelled for {SiteName}", SiteName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping {SiteName}", SiteName);
            throw;
        }
        finally
        {
            if (context != null)
            {
                await context.CloseAsync();
            }

            if (browser != null)
            {
                await browser.CloseAsync();
            }

            playwright.Dispose();
        }
    }

    private async Task<(IReadOnlyList<ScrapedListing> Listings, bool HasNext)> ScrapePageWithRetryAsync(
        IBrowserContext context,
        SearchParameters parameters,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;

        while (retryCount < MaxRetries)
        {
            IPage? page = null;

            try
            {
                page = await context.NewPageAsync();
                var url = BuildSearchUrl(parameters, pageNumber);

                _logger.LogDebug("Navigating to URL: {Url}", url);

                var response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30000
                });

                if (response != null)
                {
                    _logger.LogDebug("Navigation response: {Status} {Url}", response.Status, response.Url);
                }

                var debugEnabled = await IsDebugEnabledAsync();

                if (debugEnabled)
                {
                    await DumpDebugArtifactsAsync(page, SiteName, pageNumber, response);
                }

                if (response != null && response.Status >= 400)
                {
                    var html = await page.ContentAsync();

                    if (IsLikelyWafBlock(html))
                    {
                        _logger.LogError(
                            "Request blocked by a WAF/bot-protection page on {SiteName} (HTTP {Status}). " +
                            "This commonly requires a non-automated session (or proxy/allowlisting) to access.",
                            SiteName,
                            response.Status);

                        return (Array.Empty<ScrapedListing>(), false);
                    }

                    _logger.LogWarning(
                        "Non-success status for {SiteName} (HTTP {Status}); treating as no results.",
                        SiteName,
                        response.Status);

                    return (Array.Empty<ScrapedListing>(), false);
                }

                var listings = (await ParseListingsAsync(page, cancellationToken)).ToList();
                var hasNext = listings.Count > 0 && await HasNextPageAsync(page, cancellationToken);

                return (listings, hasNext);
            }
            catch (Exception ex) when (retryCount < MaxRetries - 1)
            {
                retryCount++;
                _logger.LogWarning(
                    ex,
                    "Error scraping page {PageNumber} (attempt {Attempt}/{MaxAttempts}). Retrying...",
                    pageNumber,
                    retryCount,
                    MaxRetries);

                await Task.Delay(RetryDelayMs * retryCount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to scrape page {PageNumber} after {MaxAttempts} attempts",
                    pageNumber,
                    MaxRetries);
                throw;
            }
            finally
            {
                if (page != null)
                {
                    await page.CloseAsync();
                }
            }
        }

        return (Array.Empty<ScrapedListing>(), false);
    }

    private static bool IsLikelyWafBlock(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        return html.Contains("_Incapsula_Resource", StringComparison.OrdinalIgnoreCase)
            || html.Contains("Incapsula incident ID", StringComparison.OrdinalIgnoreCase)
            || html.Contains("Request unsuccessful", StringComparison.OrdinalIgnoreCase);
    }

    private static Task<bool> IsDebugEnabledAsync()
    {
        var value = Environment.GetEnvironmentVariable("AMIT_SCRAPER_DEBUG");
        var enabled = string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(enabled);
    }

    private async Task DumpDebugArtifactsAsync(IPage page, string siteName, int pageNumber, IResponse? response)
    {
        try
        {
            var dumpDir = Environment.GetEnvironmentVariable("AMIT_SCRAPER_DEBUG_DIR");

            if (string.IsNullOrWhiteSpace(dumpDir))
            {
                dumpDir = Path.Combine(Path.GetTempPath(), "amit-scraper-dumps");
            }

            Directory.CreateDirectory(dumpDir);

            var safeSite = siteName.Replace('.', '_').Replace(' ', '_');
            var prefix = $"{safeSite}-page{pageNumber:00}";

            var title = await page.TitleAsync();

            _logger.LogInformation(
                "[DebugDump] {Site} page {Page} title: {Title}",
                siteName,
                pageNumber,
                title);

            if (response != null)
            {
                _logger.LogInformation(
                    "[DebugDump] {Site} page {Page} status: {Status}",
                    siteName,
                    pageNumber,
                    response.Status);
            }

            var htmlPath = Path.Combine(dumpDir, $"{prefix}.html");
            var pngPath = Path.Combine(dumpDir, $"{prefix}.png");

            var html = await page.ContentAsync();
            await File.WriteAllTextAsync(htmlPath, html);

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = pngPath,
                FullPage = true
            });

            _logger.LogInformation(
                "[DebugDump] Wrote {HtmlPath} and {PngPath}",
                htmlPath,
                pngPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DebugDump] Failed writing debug artifacts");
        }
    }
}
