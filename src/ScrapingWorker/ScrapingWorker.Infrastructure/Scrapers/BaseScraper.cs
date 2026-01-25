using Microsoft.Extensions.Logging;
using ScrapingOrchestration.Core.Enums;
using ScrapingOrchestration.Core.ValueObjects;
using ScrapingWorker.Core.Models;
using ScrapingWorker.Core.Services;
using System.Diagnostics;

namespace ScrapingWorker.Infrastructure.Scrapers;

/// <summary>
/// Base class for site scrapers with common functionality.
/// </summary>
public abstract class BaseScraper : ISiteScraper
{
    protected readonly HttpClient HttpClient;
    protected readonly IRateLimiter RateLimiter;
    protected readonly IUserAgentService UserAgentService;
    protected readonly ILogger Logger;

    public abstract ScrapingSource Source { get; }

    protected BaseScraper(
        HttpClient httpClient,
        IRateLimiter rateLimiter,
        IUserAgentService userAgentService,
        ILogger logger)
    {
        HttpClient = httpClient;
        RateLimiter = rateLimiter;
        UserAgentService = userAgentService;
        Logger = logger;
    }

    /// <inheritdoc />
    public async Task<ScrapeResult> ScrapeAsync(
        SearchParameters parameters,
        IProgress<ScrapeProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var listings = new List<ScrapedListing>();
        var warnings = new List<string>();
        var pagesCrawled = 0;

        try
        {
            Logger.LogInformation("Starting scrape for {Source} with parameters: {Make} {Model}",
                Source, parameters.Make, parameters.Model);

            await RateLimiter.WaitAsync(Source.ToString(), cancellationToken);

            var searchUrl = BuildSearchUrl(parameters);
            var totalPages = 1;
            var currentPage = 1;

            while (currentPage <= totalPages && listings.Count < parameters.MaxResults)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await RateLimiter.WaitAsync(Source.ToString(), cancellationToken);

                var pageUrl = GetPageUrl(searchUrl, currentPage);
                Logger.LogDebug("Scraping page {Page} from {Url}", currentPage, pageUrl);

                progress?.Report(new ScrapeProgress
                {
                    CurrentPage = currentPage,
                    TotalPages = totalPages,
                    ListingsScraped = listings.Count,
                    CurrentUrl = pageUrl,
                    Status = "Scraping"
                });

                try
                {
                    var (pageListings, estimatedTotal) = await ScrapePageAsync(pageUrl, cancellationToken);
                    listings.AddRange(pageListings);
                    pagesCrawled++;

                    if (currentPage == 1 && estimatedTotal > 0)
                    {
                        totalPages = Math.Min((int)Math.Ceiling(estimatedTotal / (double)GetPageSize()), 50);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error scraping page {Page}, continuing with next", currentPage);
                    warnings.Add($"Error on page {currentPage}: {ex.Message}");
                }

                currentPage++;
            }

            stopwatch.Stop();

            Logger.LogInformation(
                "Completed scrape for {Source}: {Count} listings in {Duration}ms",
                Source, listings.Count, stopwatch.ElapsedMilliseconds);

            return new ScrapeResult
            {
                Success = true,
                Listings = listings.Take(parameters.MaxResults).ToList(),
                TotalListingsFound = listings.Count,
                PagesCrawled = pagesCrawled,
                Duration = stopwatch.Elapsed,
                Warnings = warnings
            };
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Scrape cancelled for {Source}", Source);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Scrape failed for {Source}", Source);

            return ScrapeResult.Failed(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <inheritdoc />
    public virtual async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.GetAsync(GetBaseUrl(), cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Builds the search URL for the given parameters.
    /// </summary>
    protected abstract string BuildSearchUrl(SearchParameters parameters);

    /// <summary>
    /// Gets the URL for a specific page number.
    /// </summary>
    protected abstract string GetPageUrl(string baseUrl, int page);

    /// <summary>
    /// Scrapes a single page and returns the listings and estimated total count.
    /// </summary>
    protected abstract Task<(List<ScrapedListing> Listings, int EstimatedTotal)> ScrapePageAsync(
        string pageUrl,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the base URL for the source.
    /// </summary>
    protected abstract string GetBaseUrl();

    /// <summary>
    /// Gets the number of listings per page for this source.
    /// </summary>
    protected virtual int GetPageSize() => 25;

    /// <summary>
    /// Makes an HTTP request with appropriate headers.
    /// </summary>
    protected async Task<string> GetPageContentAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", UserAgentService.GetRandomUserAgent());
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        request.Headers.Add("Accept-Language", "en-CA,en-US;q=0.9,en;q=0.8");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
