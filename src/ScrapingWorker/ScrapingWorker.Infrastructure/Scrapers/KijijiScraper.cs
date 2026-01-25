using Microsoft.Extensions.Logging;
using ScrapingOrchestration.Core.Enums;
using ScrapingOrchestration.Core.ValueObjects;
using ScrapingWorker.Core.Models;
using ScrapingWorker.Core.Services;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace ScrapingWorker.Infrastructure.Scrapers;

/// <summary>
/// Scraper for Kijiji (kijiji.ca) - Canada's largest classifieds site
/// </summary>
public sealed partial class KijijiScraper : BaseScraper
{
    private const string BaseUrl = "https://www.kijiji.ca";

    public override ScrapingSource Source => ScrapingSource.Kijiji;

    public KijijiScraper(
        HttpClient httpClient,
        IRateLimiter rateLimiter,
        IUserAgentService userAgentService,
        ILogger<KijijiScraper> logger)
        : base(httpClient, rateLimiter, userAgentService, logger)
    {
    }

    protected override string GetBaseUrl() => BaseUrl;

    protected override string BuildSearchUrl(SearchParameters parameters)
    {
        // Classic Kijiji URL structure: /b-cars-trucks/canada/{make}-{model}/k0c174l0
        // Query params: yearStart=YYYY__YYYY for year range, minPrice, maxPrice, maxKilometers

        var searchTerms = new List<string>();
        if (!string.IsNullOrEmpty(parameters.Make))
        {
            searchTerms.Add(parameters.Make.ToLowerInvariant().Replace(" ", "-"));
        }
        if (!string.IsNullOrEmpty(parameters.Model))
        {
            searchTerms.Add(parameters.Model.ToLowerInvariant().Replace(" ", "-"));
        }

        var searchSlug = searchTerms.Count > 0 ? string.Join("-", searchTerms) + "/" : "";

        // c174 is the category ID for Cars & Trucks, l0 is Canada-wide
        var basePath = $"{BaseUrl}/b-cars-trucks/canada/{searchSlug}k0c174l0";

        var queryParams = new List<string> { "rb=true" }; // Enable all filters

        // Year range: format is yearStart=YYYY__YYYY
        if (parameters.YearFrom.HasValue || parameters.YearTo.HasValue)
        {
            var yearFrom = parameters.YearFrom ?? 1900;
            var yearTo = parameters.YearTo ?? DateTime.UtcNow.Year + 1;
            queryParams.Add($"yearStart={yearFrom}__{yearTo}");
        }

        if (parameters.MinPrice.HasValue)
            queryParams.Add($"minPrice={parameters.MinPrice}");

        if (parameters.MaxPrice.HasValue)
            queryParams.Add($"maxPrice={parameters.MaxPrice}");

        if (parameters.MaxMileage.HasValue)
            queryParams.Add($"maxKilometers={parameters.MaxMileage}");

        var query = string.Join("&", queryParams);
        return $"{basePath}?{query}";
    }

    protected override string GetPageUrl(string baseUrl, int page)
    {
        if (page == 1)
            return baseUrl;

        // Classic Kijiji uses page/N/ in the URL path
        // Insert before the query string if present
        var queryIndex = baseUrl.IndexOf('?');
        if (queryIndex > 0)
        {
            var pathPart = baseUrl[..queryIndex];
            var queryPart = baseUrl[queryIndex..];
            return $"{pathPart}page-{page}/{queryPart}";
        }
        return $"{baseUrl}page-{page}/";
    }

    protected override int GetPageSize() => 40; // Classic Kijiji shows ~40 items per page

    protected override async Task<(List<ScrapedListing> Listings, int EstimatedTotal)> ScrapePageAsync(
        string pageUrl,
        CancellationToken cancellationToken)
    {
        var html = await GetPageContentAsync(pageUrl, cancellationToken);
        var listings = new List<ScrapedListing>();
        var estimatedTotal = 0;

        // Extract the JSON-LD script content
        var jsonLdMatch = JsonLdRegex().Match(html);
        if (jsonLdMatch.Success)
        {
            var jsonContent = jsonLdMatch.Groups[1].Value;
            try
            {
                var parsedListings = ParseItemListJsonLd(jsonContent);
                listings.AddRange(parsedListings);
                Logger.LogDebug("Parsed {Count} listings from JSON-LD ItemList", parsedListings.Count);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error parsing JSON-LD content");
            }
        }
        else
        {
            Logger.LogDebug("No JSON-LD found on page");
        }

        // Try to extract total count from the page
        var totalMatch = TotalResultsRegex().Match(html);
        if (totalMatch.Success && int.TryParse(totalMatch.Groups[1].Value.Replace(",", ""), out var total))
        {
            estimatedTotal = total;
            Logger.LogDebug("Found {Total} total results from page text", estimatedTotal);
        }
        else
        {
            // If no total found in text, estimate based on listings
            estimatedTotal = listings.Count;
        }

        Logger.LogDebug("Parsed {Count} listings from page, estimated total: {Total}", listings.Count, estimatedTotal);

        return (listings, estimatedTotal);
    }

    private List<ScrapedListing> ParseItemListJsonLd(string json)
    {
        var listings = new List<ScrapedListing>();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Check if this is an ItemList
        if (!root.TryGetProperty("@type", out var typeElement) || typeElement.GetString() != "ItemList")
        {
            Logger.LogDebug("JSON-LD is not an ItemList, type is: {Type}", typeElement.GetString());
            return listings;
        }

        // Get the itemListElement array
        if (!root.TryGetProperty("itemListElement", out var itemListElement))
        {
            Logger.LogDebug("No itemListElement found in ItemList");
            return listings;
        }

        foreach (var listItem in itemListElement.EnumerateArray())
        {
            try
            {
                // Each listItem has @type: "ListItem" and item: { @type: "Car", ... }
                if (!listItem.TryGetProperty("item", out var item))
                {
                    continue;
                }

                var listing = ParseCarItem(item);
                if (listing != null)
                {
                    listings.Add(listing);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Error parsing individual listing from JSON-LD");
            }
        }

        return listings;
    }

    private ScrapedListing? ParseCarItem(JsonElement item)
    {
        // Verify it's a Car or Vehicle type
        if (!item.TryGetProperty("@type", out var typeEl))
        {
            return null;
        }

        var itemType = typeEl.GetString();
        if (itemType != "Car" && itemType != "Vehicle")
        {
            return null;
        }

        // Extract name
        var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        // Extract URL
        var url = item.TryGetProperty("url", out var urlEl) ? urlEl.GetString() : null;

        // Extract price from offers
        decimal? price = null;
        if (item.TryGetProperty("offers", out var offersEl))
        {
            if (offersEl.TryGetProperty("price", out var priceEl))
            {
                var priceStr = priceEl.GetString();
                if (!string.IsNullOrEmpty(priceStr) && decimal.TryParse(priceStr, out var p))
                {
                    price = p;
                }
            }
        }

        // Extract year from vehicleModelDate
        int? year = null;
        if (item.TryGetProperty("vehicleModelDate", out var yearEl))
        {
            var yearStr = yearEl.GetString();
            if (!string.IsNullOrEmpty(yearStr) && int.TryParse(yearStr, out var y))
            {
                year = y;
            }
        }

        // Extract mileage from mileageFromOdometer
        int? mileage = null;
        if (item.TryGetProperty("mileageFromOdometer", out var mileageEl))
        {
            if (mileageEl.TryGetProperty("value", out var mileageValEl))
            {
                var mileageStr = mileageValEl.GetString();
                if (!string.IsNullOrEmpty(mileageStr) && int.TryParse(mileageStr.Replace(",", ""), out var m))
                {
                    mileage = m;
                }
            }
        }

        // Extract image
        var imageUrl = item.TryGetProperty("image", out var imageEl) ? imageEl.GetString() : null;

        // Extract VIN
        var vin = item.TryGetProperty("vehicleIdentificationNumber", out var vinEl) ? vinEl.GetString() : null;

        // Extract make from brand
        string? make = null;
        if (item.TryGetProperty("brand", out var brandEl))
        {
            make = brandEl.TryGetProperty("name", out var brandNameEl) ? brandNameEl.GetString() : null;
        }

        // Extract model
        var model = item.TryGetProperty("model", out var modelEl) ? modelEl.GetString() : null;

        // Extract color
        var exteriorColor = item.TryGetProperty("color", out var colorEl) ? colorEl.GetString() : null;

        // Extract body style
        var bodyStyle = item.TryGetProperty("bodyType", out var bodyEl) ? bodyEl.GetString() : null;

        // Extract transmission
        var transmission = item.TryGetProperty("vehicleTransmission", out var transEl) ? transEl.GetString() : null;

        // Extract fuel type from engine
        string? fuelType = null;
        if (item.TryGetProperty("vehicleEngine", out var engineEl))
        {
            fuelType = engineEl.TryGetProperty("fuelType", out var fuelEl) ? fuelEl.GetString() : null;
        }

        // Extract description
        var description = item.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;

        // Generate a unique ID from the URL or VIN
        var listingId = !string.IsNullOrEmpty(vin) ? vin : ExtractListingIdFromUrl(url ?? "");

        return new ScrapedListing
        {
            SourceListingId = listingId,
            Source = ScrapingSource.Kijiji,
            Title = HttpUtility.HtmlDecode(name),
            Price = price,
            Year = year,
            Make = make,
            Model = model,
            Mileage = mileage,
            VIN = vin,
            ExteriorColor = exteriorColor,
            BodyStyle = bodyStyle,
            Transmission = transmission,
            FuelType = fuelType,
            Description = description != null ? HttpUtility.HtmlDecode(description) : null,
            ListingUrl = url ?? BaseUrl,
            ImageUrls = !string.IsNullOrEmpty(imageUrl) ? new[] { imageUrl } : Array.Empty<string>(),
            ScrapedAt = DateTimeOffset.UtcNow
        };
    }

    private static string ExtractListingIdFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return Guid.NewGuid().ToString("N");

        // Kijiji URLs often contain the listing ID at the end
        // e.g., /v-cars-trucks/city/title/1234567
        var match = ListingIdFromUrlRegex().Match(url);
        return match.Success ? match.Groups[1].Value : url.GetHashCode().ToString();
    }

    // Regex patterns
    [GeneratedRegex(@"(\d{1,3}(?:,\d{3})*)\s*(?:results?|vehicles?|listings?|cars?|ads?)", RegexOptions.IgnoreCase)]
    private static partial Regex TotalResultsRegex();

    [GeneratedRegex(@"<script[^>]*type=""application/ld\+json""[^>]*>(\{""@context""[^<]+)</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex JsonLdRegex();

    [GeneratedRegex(@"/(\d+)(?:\?|$)")]
    private static partial Regex ListingIdFromUrlRegex();
}
