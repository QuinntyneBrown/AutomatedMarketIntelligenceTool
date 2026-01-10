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

        var headless = GetEnvBool("AMIT_SCRAPER_HEADLESS", defaultValue: true);
        var slowMoMs = GetEnvInt("AMIT_SCRAPER_SLOWMO_MS", defaultValue: 0);
        var storageStatePath = Environment.GetEnvironmentVariable("AMIT_SCRAPER_STORAGE_STATE");
        var saveStorageStatePath = Environment.GetEnvironmentVariable("AMIT_SCRAPER_SAVE_STORAGE_STATE");

        // Convenience: if the user only provides a save path, use it as the load path too.
        if (string.IsNullOrWhiteSpace(storageStatePath) && !string.IsNullOrWhiteSpace(saveStorageStatePath))
        {
            storageStatePath = saveStorageStatePath;
        }
        var interactiveOnBotBlock = GetEnvBool("AMIT_SCRAPER_INTERACTIVE_ON_BOT_BLOCK", defaultValue: false);
        var interactiveTimeoutSeconds = GetEnvInt("AMIT_SCRAPER_INTERACTIVE_TIMEOUT_SECONDS", defaultValue: 180);
        var interactiveSiteAllowlist = Environment.GetEnvironmentVariable("AMIT_SCRAPER_INTERACTIVE_SITE_ALLOWLIST");
        var switchedToInteractive = false;

        BrowserNewContextOptions CreateContextOptions(bool includeStorageState)
        {
            var options = new BrowserNewContextOptions
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
                    ["Accept"] =
                        "text/html,application/xhtml+xml,application/xml;q=0.9," +
                        "image/avif,image/webp,image/apng,*/*;q=0.8",
                    ["Accept-Language"] = "en-CA,en;q=0.9"
                }
            };

            if (includeStorageState && !string.IsNullOrWhiteSpace(storageStatePath))
            {
                if (File.Exists(storageStatePath))
                {
                    options.StorageStatePath = storageStatePath;
                    _logger.LogInformation("Loaded Playwright storage state from {Path}", storageStatePath);
                }
                else
                {
                    _logger.LogWarning(
                        "AMIT_SCRAPER_STORAGE_STATE was set but file does not exist: {Path}",
                        storageStatePath);
                }
            }

            return options;
        }

        async Task<(IBrowser Browser, IBrowserContext Context)> CreateBrowserAndContextAsync(
            bool runHeadless,
            bool includeStorageState)
        {
            var createdBrowser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = runHeadless,
                SlowMo = slowMoMs > 0 ? slowMoMs : null,
                Args = new[] { "--disable-blink-features=AutomationControlled" }
            });

            var createdContext = await createdBrowser.NewContextAsync(CreateContextOptions(includeStorageState));

            await createdContext.AddInitScriptAsync(
                "Object.defineProperty(navigator, 'webdriver', { get: () => undefined });");

            return (createdBrowser, createdContext);
        }

        try
        {
            (browser, context) = await CreateBrowserAndContextAsync(headless, includeStorageState: true);

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

                (IReadOnlyList<ScrapedListing> Listings, bool HasNext) pageListings;
                try
                {
                    pageListings = await ScrapePageWithRetryAsync(
                        context,
                        parameters,
                        currentPage,
                        cancellationToken);
                }
                catch (BotProtectionException ex)
                {
                    var allowInteractiveForThisSite = IsInteractiveAllowedForSite(SiteName, interactiveSiteAllowlist);

                    if (headless && interactiveOnBotBlock && allowInteractiveForThisSite && !switchedToInteractive)
                    {
                        switchedToInteractive = true;
                        _logger.LogWarning(
                            "Encountered {Provider} bot-protection on {SiteName}. Switching to interactive browser to allow manual CAPTCHA completion (timeout {TimeoutSeconds}s).",
                            ex.Provider,
                            SiteName,
                            interactiveTimeoutSeconds);

                        await SwitchToInteractiveAndWaitAsync(
                            url: ex.Url,
                            provider: ex.Provider,
                            timeoutSeconds: interactiveTimeoutSeconds,
                            cancellationToken: cancellationToken);

                        // After interactive wait, recreate an interactive (headful) browser/context and retry this page.
                        if (context != null)
                        {
                            await context.CloseAsync();
                        }

                        if (browser != null)
                        {
                            await browser.CloseAsync();
                        }

                        headless = false;
                        (browser, context) = await CreateBrowserAndContextAsync(headless, includeStorageState: true);

                        pageListings = await ScrapePageWithRetryAsync(
                            context,
                            parameters,
                            currentPage,
                            cancellationToken);
                    }
                    else
                    {
                        _logger.LogError(
                            "Request blocked by {Provider} bot-protection on {SiteName} (HTTP {Status}). " +
                            "Enable AMIT_SCRAPER_INTERACTIVE_ON_BOT_BLOCK=1 to switch to a visible browser for manual CAPTCHA completion. " +
                            "Optionally set AMIT_SCRAPER_INTERACTIVE_SITE_ALLOWLIST to a comma-separated list (e.g. 'CarGurus') to limit interactive fallback.",
                            ex.Provider,
                            SiteName,
                            ex.Status);

                        return allListings;
                    }
                }

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

            if (context != null && !string.IsNullOrWhiteSpace(saveStorageStatePath))
            {
                await context.StorageStateAsync(new BrowserContextStorageStateOptions
                {
                    Path = saveStorageStatePath
                });

                _logger.LogInformation("Saved Playwright storage state to {Path}", saveStorageStatePath);
            }

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

                // Some bot-protection/challenge pages return HTTP 200 (e.g. AWS WAF, Cloudflare).
                // Detect these early so the caller can optionally switch to an interactive browser.
                {
                    var html = await page.ContentAsync();
                    var botBlockProvider = DetectBotBlockProvider(html);
                    if (botBlockProvider != null)
                    {
                        throw new BotProtectionException(
                            provider: botBlockProvider,
                            url: url,
                            status: response?.Status ?? 0);
                    }
                }

                if (response != null && response.Status >= 400)
                {
                    var html = await page.ContentAsync();

                    var botBlockProvider = DetectBotBlockProvider(html);
                    if (botBlockProvider != null)
                    {
                        throw new BotProtectionException(
                            provider: botBlockProvider,
                            url: url,
                            status: response.Status);
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
            catch (BotProtectionException)
            {
                // Bot-protection blocks are not transient in a headless retry loop; let the caller decide.
                throw;
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

    private async Task SwitchToInteractiveAndWaitAsync(
        string url,
        string provider,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        // Launch a short-lived headful browser for the user to complete CAPTCHA.
        // We do not attempt to bypass bot protection; this is manual, opt-in assistance.
        var playwright = await Playwright.CreateAsync();
        try
        {
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[] { "--disable-blink-features=AutomationControlled" }
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "en-CA",
                TimezoneId = "America/Toronto",
                ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                    "AppleWebKit/537.36 (KHTML, like Gecko) " +
                    "Chrome/120.0.0.0 Safari/537.36",
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept"] =
                        "text/html,application/xhtml+xml,application/xml;q=0.9," +
                        "image/avif,image/webp,image/apng,*/*;q=0.8",
                    ["Accept-Language"] = "en-CA,en;q=0.9"
                }
            });

            await context.AddInitScriptAsync(
                "Object.defineProperty(navigator, 'webdriver', { get: () => undefined });");

            var page = await context.NewPageAsync();

            _logger.LogWarning(
                "Interactive browser opened for {Provider}. Please complete any CAPTCHA in the browser window. Waiting up to {TimeoutSeconds}s...",
                provider,
                timeoutSeconds);

            Console.WriteLine("Press SPACEBAR if no CAPTCHA is shown and you want to continue immediately.");

            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30000
            });

            var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if user pressed spacebar to skip waiting
                try
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        if (key.Key == ConsoleKey.Spacebar)
                        {
                            _logger.LogInformation("Spacebar pressed; continuing without waiting for bot-protection detection.");
                            break;
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // Console input may not be available in all environments; ignore
                }

                try
                {
                    // The page may be auto-refreshing / navigating while the challenge is solved.
                    // We treat transient navigation issues as "still waiting" rather than failing the whole scrape.
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions
                    {
                        Timeout = 5000
                    });

                    var html = await page.ContentAsync();
                    if (DetectBotBlockProvider(html) == null)
                    {
                        _logger.LogInformation("Bot-protection page no longer detected; continuing.");
                        break;
                    }
                }
                catch (PlaywrightException ex)
                {
                    if (ex.Message.Contains("Target page, context or browser has been closed", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "Interactive browser window was closed; continuing without interactive clearance.");
                        return;
                    }

                    // Common during navigation/challenge refresh.
                    _logger.LogDebug(ex, "Interactive wait encountered a transient Playwright error; continuing to wait.");
                }

                await Task.Delay(1000, cancellationToken);
            }

            var saveStorageStatePath = Environment.GetEnvironmentVariable("AMIT_SCRAPER_SAVE_STORAGE_STATE");
            if (!string.IsNullOrWhiteSpace(saveStorageStatePath))
            {
                await context.StorageStateAsync(new BrowserContextStorageStateOptions
                {
                    Path = saveStorageStatePath
                });

                _logger.LogInformation("Saved Playwright storage state to {Path}", saveStorageStatePath);
            }

            await page.CloseAsync();
            await context.CloseAsync();
            await browser.CloseAsync();
        }
        finally
        {
            playwright.Dispose();
        }
    }

    private static bool IsInteractiveAllowedForSite(string siteName, string? allowlist)
    {
        if (string.IsNullOrWhiteSpace(allowlist))
        {
            return true;
        }

        var allowed = allowlist
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return allowed.Any(s => string.Equals(s, siteName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLikelyWafBlock(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        return html.Contains("Incapsula incident ID", StringComparison.OrdinalIgnoreCase)
            || html.Contains("Request unsuccessful", StringComparison.OrdinalIgnoreCase)
            || html.Contains("captcha-delivery.com", StringComparison.OrdinalIgnoreCase)
            || html.Contains("captcha-sdk.awswaf.com", StringComparison.OrdinalIgnoreCase)
            || html.Contains("AwsWAFScript", StringComparison.OrdinalIgnoreCase)
            || html.Contains("cf-chl", StringComparison.OrdinalIgnoreCase)
            || html.Contains("Just a moment", StringComparison.OrdinalIgnoreCase)
            || html.Contains("verify you are human", StringComparison.OrdinalIgnoreCase)
            || html.Contains("Access Denied", StringComparison.OrdinalIgnoreCase);
    }

    private static string? DetectBotBlockProvider(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        // DataDome: avoid false positives from generic vendor scripts by requiring CAPTCHA delivery markers.
        if (html.Contains("captcha-delivery.com", StringComparison.OrdinalIgnoreCase)
            || (html.Contains("datadome", StringComparison.OrdinalIgnoreCase)
                && html.Contains("captcha", StringComparison.OrdinalIgnoreCase)))
        {
            return "DataDome";
        }

        if (html.Contains("captcha-sdk.awswaf.com", StringComparison.OrdinalIgnoreCase)
            || html.Contains("AwsWAFScript", StringComparison.OrdinalIgnoreCase)
            || html.Contains("awswaf", StringComparison.OrdinalIgnoreCase))
        {
            return "AWS WAF";
        }

        // Incapsula: don't treat the presence of Incapsula resources alone as a block.
        if (html.Contains("Incapsula incident ID", StringComparison.OrdinalIgnoreCase)
            || html.Contains("Request unsuccessful", StringComparison.OrdinalIgnoreCase))
        {
            return "Incapsula";
        }

        // Cloudflare: challenge pages often include cf-chl markers / "Just a moment".
        if (html.Contains("cf-chl", StringComparison.OrdinalIgnoreCase)
            || html.Contains("Just a moment", StringComparison.OrdinalIgnoreCase)
            || (html.Contains("Cloudflare", StringComparison.OrdinalIgnoreCase)
                && html.Contains("captcha", StringComparison.OrdinalIgnoreCase)))
        {
            return "Cloudflare";
        }

        return IsLikelyWafBlock(html) ? "bot-protection" : null;
    }

    private static bool GetEnvBool(string name, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "0" or "false" or "no" or "off" => false,
            "1" or "true" or "yes" or "on" => true,
            _ => defaultValue
        };
    }

    private static int GetEnvInt(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private sealed class BotProtectionException : Exception
    {
        public BotProtectionException(string provider, string url, int status)
            : base($"Blocked by {provider} (HTTP {status})")
        {
            Provider = provider;
            Url = url;
            Status = status;
        }

        public string Provider { get; }
        public string Url { get; }
        public int Status { get; }
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

    #region Shared Parsing Helpers

    /// <summary>
    /// Parses a price string by removing currency symbols and formatting.
    /// Override in derived classes for site-specific handling.
    /// </summary>
    protected virtual decimal ParsePrice(string priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText))
        {
            return 0;
        }

        var cleanPrice = priceText
            .Replace("$", string.Empty)
            .Replace(",", string.Empty)
            .Replace("CAD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("USD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return decimal.TryParse(cleanPrice, out var price) ? price : 0;
    }

    /// <summary>
    /// Parses a mileage string by removing units and formatting.
    /// Override in derived classes for site-specific handling.
    /// </summary>
    protected virtual int? ParseMileage(string mileageText)
    {
        if (string.IsNullOrWhiteSpace(mileageText))
        {
            return null;
        }

        var cleanMileage = mileageText
            .Replace(",", string.Empty)
            .Replace("km", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("kilometers", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("kilometres", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("mi", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("miles", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return int.TryParse(cleanMileage, out var mileage) ? mileage : null;
    }

    /// <summary>
    /// Parses a location string into city and province components.
    /// Expects format "City, Province" or similar.
    /// </summary>
    protected virtual (string? City, string? Province) ParseLocation(string locationText)
    {
        if (string.IsNullOrWhiteSpace(locationText))
        {
            return (null, null);
        }

        var parts = locationText.Split(',');
        if (parts.Length >= 2)
        {
            return (parts[0].Trim(), parts[1].Trim());
        }

        if (parts.Length == 1 && !string.IsNullOrWhiteSpace(parts[0]))
        {
            return (parts[0].Trim(), null);
        }

        return (null, null);
    }

    /// <summary>
    /// Parses a vehicle title string into make, model, and year.
    /// Expects format "Year Make Model [Trim]" (e.g., "2020 Toyota Camry LE").
    /// </summary>
    protected virtual (string Make, string Model, int Year) ParseTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (string.Empty, string.Empty, 0);
        }

        var parts = title.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
        {
            return (string.Empty, string.Empty, 0);
        }

        if (int.TryParse(parts[0], out var year) && year >= 1900 && year <= DateTime.Now.Year + 2)
        {
            var make = parts[1];
            var model = string.Join(" ", parts.Skip(2));
            return (make, model, year);
        }

        return (string.Empty, string.Empty, 0);
    }

    /// <summary>
    /// Extracts an external ID from a listing URL.
    /// Override in derived classes for site-specific URL patterns.
    /// </summary>
    protected virtual string ExtractExternalId(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        try
        {
            var uri = new Uri(url);
            var lastSegment = uri.Segments
                .Select(s => s.Trim('/'))
                .LastOrDefault(s => !string.IsNullOrWhiteSpace(s));

            if (!string.IsNullOrWhiteSpace(lastSegment))
            {
                return lastSegment;
            }
        }
        catch
        {
            // ignored
        }

        return url.GetHashCode().ToString();
    }

    #endregion
}
