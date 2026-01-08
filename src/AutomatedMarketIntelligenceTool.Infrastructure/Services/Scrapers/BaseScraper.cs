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

        try
        {
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

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
                    browser,
                    parameters,
                    currentPage,
                    cancellationToken);

                if (pageListings == null || !pageListings.Any())
                {
                    _logger.LogInformation(
                        "No listings found on page {CurrentPage}, ending pagination",
                        currentPage);
                    break;
                }

                allListings.AddRange(pageListings);

                progress?.Report(new ScrapeProgress
                {
                    SiteName = SiteName,
                    CurrentPage = currentPage,
                    TotalListingsFound = allListings.Count,
                    Message = $"Found {pageListings.Count()} listings on page {currentPage}"
                });

                var hasNext = await HasNextPageWithRetryAsync(
                    browser,
                    parameters,
                    currentPage,
                    cancellationToken);

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
            if (browser != null)
            {
                await browser.CloseAsync();
            }

            playwright.Dispose();
        }
    }

    private async Task<IEnumerable<ScrapedListing>?> ScrapePageWithRetryAsync(
        IBrowser browser,
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
                page = await browser.NewPageAsync();
                var url = BuildSearchUrl(parameters, pageNumber);

                _logger.LogDebug("Navigating to URL: {Url}", url);

                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000
                });

                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                var listings = await ParseListingsAsync(page, cancellationToken);
                return listings;
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

        return null;
    }

    private async Task<bool> HasNextPageWithRetryAsync(
        IBrowser browser,
        SearchParameters parameters,
        int currentPage,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;

        while (retryCount < MaxRetries)
        {
            IPage? page = null;

            try
            {
                page = await browser.NewPageAsync();
                var url = BuildSearchUrl(parameters, currentPage);

                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000
                });

                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                return await HasNextPageAsync(page, cancellationToken);
            }
            catch (Exception ex) when (retryCount < MaxRetries - 1)
            {
                retryCount++;
                _logger.LogWarning(
                    ex,
                    "Error checking next page (attempt {Attempt}/{MaxAttempts}). Retrying...",
                    retryCount,
                    MaxRetries);

                await Task.Delay(RetryDelayMs * retryCount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check if next page exists, assuming no more pages");
                return false;
            }
            finally
            {
                if (page != null)
                {
                    await page.CloseAsync();
                }
            }
        }

        return false;
    }
}
