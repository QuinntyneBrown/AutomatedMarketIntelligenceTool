using Microsoft.Extensions.Logging;
using ScrapingOrchestration.Core.Enums;
using ScrapingOrchestration.Core.ValueObjects;
using ScrapingWorker.Core.Models;
using ScrapingWorker.Core.Services;
using System.Text.RegularExpressions;
using System.Web;

namespace ScrapingWorker.Infrastructure.Scrapers;

/// <summary>
/// Scraper for Autotrader.ca
/// </summary>
public sealed partial class AutotraderScraper : BaseScraper
{
    private const string BaseUrl = "https://www.autotrader.ca";

    public override ScrapingSource Source => ScrapingSource.Autotrader;

    public AutotraderScraper(
        HttpClient httpClient,
        IRateLimiter rateLimiter,
        IUserAgentService userAgentService,
        ILogger<AutotraderScraper> logger)
        : base(httpClient, rateLimiter, userAgentService, logger)
    {
    }

    protected override string GetBaseUrl() => BaseUrl;

    protected override string BuildSearchUrl(SearchParameters parameters)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(parameters.Make))
            queryParams.Add($"mk={HttpUtility.UrlEncode(parameters.Make)}");

        if (!string.IsNullOrEmpty(parameters.Model))
            queryParams.Add($"md={HttpUtility.UrlEncode(parameters.Model)}");

        if (parameters.YearFrom.HasValue)
            queryParams.Add($"yRng={parameters.YearFrom}%2C{parameters.YearTo ?? DateTime.UtcNow.Year + 1}");

        if (parameters.MinPrice.HasValue || parameters.MaxPrice.HasValue)
        {
            var minPrice = parameters.MinPrice ?? 0;
            var maxPrice = parameters.MaxPrice ?? 999999;
            queryParams.Add($"pRng={minPrice}%2C{maxPrice}");
        }

        if (parameters.MaxMileage.HasValue)
            queryParams.Add($"odRng=%2C{parameters.MaxMileage}");

        if (!string.IsNullOrEmpty(parameters.PostalCode))
        {
            queryParams.Add($"loc={HttpUtility.UrlEncode(parameters.PostalCode)}");
            if (parameters.RadiusKm.HasValue)
                queryParams.Add($"prx={parameters.RadiusKm}");
        }

        if (!string.IsNullOrEmpty(parameters.Province))
            queryParams.Add($"prv={HttpUtility.UrlEncode(parameters.Province)}");

        var query = string.Join("&", queryParams);
        return $"{BaseUrl}/cars/?{query}";
    }

    protected override string GetPageUrl(string baseUrl, int page)
    {
        if (page == 1)
            return baseUrl;

        var separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}rcp=15&rcs={(page - 1) * 15}";
    }

    protected override int GetPageSize() => 15;

    protected override async Task<(List<ScrapedListing> Listings, int EstimatedTotal)> ScrapePageAsync(
        string pageUrl,
        CancellationToken cancellationToken)
    {
        var html = await GetPageContentAsync(pageUrl, cancellationToken);
        var listings = new List<ScrapedListing>();
        var estimatedTotal = 0;

        // Parse total results count
        var totalMatch = TotalResultsRegex().Match(html);
        if (totalMatch.Success && int.TryParse(totalMatch.Groups[1].Value.Replace(",", ""), out var total))
        {
            estimatedTotal = total;
        }

        // Parse listings - this is a simplified implementation
        // Real implementation would use proper HTML parsing
        var listingMatches = ListingBlockRegex().Matches(html);

        foreach (Match match in listingMatches)
        {
            try
            {
                var listing = ParseListing(match.Value);
                if (listing != null)
                {
                    listings.Add(listing);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error parsing listing block");
            }
        }

        Logger.LogDebug("Parsed {Count} listings from page, estimated total: {Total}", listings.Count, estimatedTotal);

        return (listings, estimatedTotal);
    }

    private ScrapedListing? ParseListing(string html)
    {
        // This is a placeholder implementation
        // Real implementation would use proper HTML parsing library like AngleSharp
        var idMatch = ListingIdRegex().Match(html);
        if (!idMatch.Success)
            return null;

        var titleMatch = TitleRegex().Match(html);
        var priceMatch = PriceRegex().Match(html);
        var mileageMatch = MileageRegex().Match(html);

        var title = titleMatch.Success ? HttpUtility.HtmlDecode(titleMatch.Groups[1].Value) : "Unknown";

        decimal? price = null;
        if (priceMatch.Success && decimal.TryParse(priceMatch.Groups[1].Value.Replace(",", ""), out var parsedPrice))
        {
            price = parsedPrice;
        }

        int? mileage = null;
        if (mileageMatch.Success && int.TryParse(mileageMatch.Groups[1].Value.Replace(",", ""), out var parsedMileage))
        {
            mileage = parsedMileage;
        }

        // Extract year, make, model from title
        var yearMatch = YearRegex().Match(title);
        int? year = yearMatch.Success && int.TryParse(yearMatch.Value, out var y) ? y : null;

        return new ScrapedListing
        {
            SourceListingId = idMatch.Groups[1].Value,
            Source = ScrapingSource.Autotrader,
            Title = title,
            Price = price,
            Year = year,
            Mileage = mileage,
            ListingUrl = $"{BaseUrl}/a/{idMatch.Groups[1].Value}",
            ScrapedAt = DateTimeOffset.UtcNow
        };
    }

    [GeneratedRegex(@"(\d{1,3}(?:,\d{3})*)\s*(?:results|listings)", RegexOptions.IgnoreCase)]
    private static partial Regex TotalResultsRegex();

    [GeneratedRegex(@"<div[^>]*class=""[^""]*result-item[^""]*""[^>]*>.*?</div>\s*</div>", RegexOptions.Singleline)]
    private static partial Regex ListingBlockRegex();

    [GeneratedRegex(@"data-listing-id=""(\d+)""")]
    private static partial Regex ListingIdRegex();

    [GeneratedRegex(@"<h2[^>]*>([^<]+)</h2>")]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"\$\s*([\d,]+)")]
    private static partial Regex PriceRegex();

    [GeneratedRegex(@"([\d,]+)\s*km", RegexOptions.IgnoreCase)]
    private static partial Regex MileageRegex();

    [GeneratedRegex(@"\b(19|20)\d{2}\b")]
    private static partial Regex YearRegex();
}
