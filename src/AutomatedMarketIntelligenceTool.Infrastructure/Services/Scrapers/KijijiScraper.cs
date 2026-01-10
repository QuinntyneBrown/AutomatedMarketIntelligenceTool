using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class KijijiScraper : BaseScraper
{
    public override string SiteName => "Kijiji.ca";

    public KijijiScraper(ILogger<KijijiScraper> logger)
        : base(logger)
    {
    }

    protected override string BuildSearchUrl(SearchParameters parameters, int page)
    {
        // Kijiji SRP has moved away from the old query-param based make/model filters.
        // A more stable approach is to use keyword search paths.
        var location = parameters.Province?.ToString().ToLowerInvariant() ?? "canada";
        var keywords = BuildKeywordSlug(parameters);

        var baseUrl = new StringBuilder("https://www.kijiji.ca/b-cars-trucks/");
        baseUrl.Append(location);
        baseUrl.Append('/');

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            baseUrl.Append(keywords);
            baseUrl.Append('/');
        }

        if (page > 1)
        {
            baseUrl.Append($"page-{page}/");
        }

        // Category code for Cars & Trucks.
        baseUrl.Append("k0c174l0");

        // Preserve support for filters via query parameters.
        var queryParams = new List<string>();

        if (parameters.YearMin.HasValue)
        {
            queryParams.Add($"carypmin={parameters.YearMin.Value}");
        }

        if (parameters.YearMax.HasValue)
        {
            queryParams.Add($"carypmax={parameters.YearMax.Value}");
        }

        if (parameters.PriceMin.HasValue)
        {
            queryParams.Add($"pricemin={decimal.ToInt32(parameters.PriceMin.Value)}");
        }

        if (parameters.PriceMax.HasValue)
        {
            queryParams.Add($"pricemax={decimal.ToInt32(parameters.PriceMax.Value)}");
        }

        if (parameters.MileageMax.HasValue)
        {
            queryParams.Add($"carod={parameters.MileageMax.Value}");
        }

        if (!string.IsNullOrWhiteSpace(parameters.PostalCode))
        {
            var normalizedPostal = Regex.Replace(parameters.PostalCode, "[^A-Za-z0-9]+", string.Empty)
                .ToLowerInvariant();
            queryParams.Add($"postalCode={HttpUtility.UrlEncode(normalizedPostal)}");
        }

        if (parameters.RadiusKilometers.HasValue)
        {
            queryParams.Add($"radius={parameters.RadiusKilometers.Value}");
        }

        var url = baseUrl.ToString();
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return url;
    }

    protected override async Task<IEnumerable<ScrapedListing>> ParseListingsAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        var listings = new List<ScrapedListing>();

        try
        {
            // Prefer JSON-LD if present (works better with modern Next.js markup).
            var jsonLdListings = await TryParseListingsFromJsonLdAsync(page, cancellationToken);
            if (jsonLdListings.Count > 0)
            {
                return jsonLdListings;
            }

            // Fallback: legacy markup.
            await page.WaitForSelectorAsync(".search-item", new PageWaitForSelectorOptions { Timeout = 10000 });
            var listingElements = await page.QuerySelectorAllAsync(".search-item");

            _logger.LogDebug("Found {Count} listing elements on page", listingElements.Count);

            foreach (var element in listingElements)
            {
                try
                {
                    var listing = await ParseListingElementAsync(element, cancellationToken);
                    if (listing != null)
                    {
                        listings.Add(listing);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse individual listing element");
                }
            }
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout waiting for listing elements, page may be empty");
        }

        return listings;
    }

    private async Task<List<ScrapedListing>> TryParseListingsFromJsonLdAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        var results = new List<ScrapedListing>();

        try
        {
            var scripts = await page.QuerySelectorAllAsync("script[type='application/ld+json']");
            foreach (var script in scripts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var json = await script.InnerTextAsync();
                if (string.IsNullOrWhiteSpace(json))
                {
                    continue;
                }

                foreach (var itemList in ExtractItemLists(json))
                {
                    foreach (var item in itemList)
                    {
                        if (string.IsNullOrWhiteSpace(item.Url) || string.IsNullOrWhiteSpace(item.Name))
                        {
                            continue;
                        }

                        var (make, model, year) = ParseTitle(item.Name);
                        if (string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model) || year == 0)
                        {
                            continue;
                        }

                        results.Add(new ScrapedListing
                        {
                            ExternalId = ExtractExternalId(item.Url),
                            SourceSite = SiteName,
                            ListingUrl = item.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                ? item.Url
                                : $"https://www.kijiji.ca{item.Url}",
                            Make = make,
                            Model = model,
                            Year = year,
                            Price = item.Price ?? 0,
                            Mileage = null,
                            City = null,
                            Province = null,
                            Condition = Condition.Used
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed JSON-LD parse; falling back to DOM");
        }

        return results;
    }

    private static string BuildKeywordSlug(SearchParameters parameters)
    {
        var keyword = string.Join(' ', new[] { parameters.Make, parameters.Model }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return string.Empty;
        }

        var lower = keyword.Trim().ToLowerInvariant();
        lower = Regex.Replace(lower, "[^a-z0-9]+", "-");
        lower = Regex.Replace(lower, "-+", "-").Trim('-');
        return Uri.EscapeDataString(lower);
    }

    private static IEnumerable<List<(string Url, string Name, decimal? Price)>> ExtractItemLists(string json)
    {
        using var doc = JsonDocument.Parse(json);
        foreach (var list in ExtractItemLists(doc.RootElement))
        {
            yield return list;
        }
    }

    private static IEnumerable<List<(string Url, string Name, decimal? Price)>> ExtractItemLists(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                foreach (var list in ExtractItemLists(child))
                {
                    yield return list;
                }
            }

            yield break;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        if (element.TryGetProperty("@type", out var typeProp) &&
            typeProp.ValueKind == JsonValueKind.String &&
            string.Equals(typeProp.GetString(), "ItemList", StringComparison.OrdinalIgnoreCase) &&
            element.TryGetProperty("itemListElement", out var itemsProp) &&
            itemsProp.ValueKind == JsonValueKind.Array)
        {
            var items = new List<(string Url, string Name, decimal? Price)>();

            foreach (var entry in itemsProp.EnumerateArray())
            {
                // Common shapes:
                // { "@type":"ListItem", "item": { "url":"...", "name":"...", "offers": {"price": 12345 } } }
                // or { "item": { ... } }
                var itemObj = entry;
                if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("item", out var nestedItem))
                {
                    itemObj = nestedItem;
                }

                if (itemObj.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var url = itemObj.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String
                    ? urlProp.GetString() ?? string.Empty
                    : string.Empty;

                var name = itemObj.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
                    ? nameProp.GetString() ?? string.Empty
                    : string.Empty;

                decimal? price = null;
                if (itemObj.TryGetProperty("offers", out var offersProp) && offersProp.ValueKind == JsonValueKind.Object)
                {
                    if (offersProp.TryGetProperty("price", out var priceProp))
                    {
                        if (priceProp.ValueKind == JsonValueKind.Number && priceProp.TryGetDecimal(out var p))
                        {
                            price = p;
                        }
                        else if (priceProp.ValueKind == JsonValueKind.String && decimal.TryParse(priceProp.GetString(), out var ps))
                        {
                            price = ps;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(name))
                {
                    items.Add((url, name, price));
                }
            }

            if (items.Count > 0)
            {
                yield return items;
            }
        }

        foreach (var prop in element.EnumerateObject())
        {
            foreach (var list in ExtractItemLists(prop.Value))
            {
                yield return list;
            }
        }
    }

    private async Task<ScrapedListing?> ParseListingElementAsync(
        IElementHandle element,
        CancellationToken cancellationToken)
    {
        try
        {
            var titleElement = await element.QuerySelectorAsync(".title");
            var title = titleElement != null ? await titleElement.InnerTextAsync() : string.Empty;

            var priceElement = await element.QuerySelectorAsync(".price");
            var priceText = priceElement != null ? await priceElement.InnerTextAsync() : "0";
            var price = ParsePrice(priceText);

            var linkElement = await element.QuerySelectorAsync("a.title");
            var href = linkElement != null ? await linkElement.GetAttributeAsync("href") ?? string.Empty : string.Empty;
            var listingUrl = href.StartsWith("http") ? href : $"https://www.kijiji.ca{href}";

            var externalId = ExtractExternalId(listingUrl);

            var detailsElement = await element.QuerySelectorAsync(".details");
            var detailsText = detailsElement != null ? await detailsElement.InnerTextAsync() : string.Empty;
            var mileage = ParseMileage(detailsText);

            var locationElement = await element.QuerySelectorAsync(".location span");
            var locationText = locationElement != null ? await locationElement.InnerTextAsync() : string.Empty;
            var (city, province) = ParseLocation(locationText);

            var (make, model, year) = ParseTitle(title);

            if (string.IsNullOrEmpty(make) || string.IsNullOrEmpty(model) || year == 0)
            {
                _logger.LogWarning("Failed to parse required fields from title: {Title}", title);
                return null;
            }

            return new ScrapedListing
            {
                ExternalId = externalId,
                SourceSite = SiteName,
                ListingUrl = listingUrl,
                Make = make,
                Model = model,
                Year = year,
                Price = price,
                Mileage = mileage,
                City = city,
                Province = province,
                Condition = Condition.Used
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing listing element");
            return null;
        }
    }

    protected override async Task<bool> HasNextPageAsync(IPage page, CancellationToken cancellationToken)
    {
        try
        {
            var nextButton = await page.QuerySelectorAsync("a[title='Next']");
            return nextButton != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for next page");
            return false;
        }
    }

    protected override string ExtractExternalId(string url)
    {
        var parts = url.Split('/');
        foreach (var part in parts)
        {
            // Kijiji URLs often have IDs starting with "v-" or numeric IDs
            if (part.StartsWith("v-") || (long.TryParse(part, out _) && part.Length > 5))
            {
                return part;
            }
        }

        return base.ExtractExternalId(url);
    }

    protected override decimal ParsePrice(string priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText))
        {
            return 0;
        }

        // Handle various "contact for price" phrases (English and French)
        if (priceText.Contains("Contact", StringComparison.OrdinalIgnoreCase) ||
            priceText.Contains("Call", StringComparison.OrdinalIgnoreCase) ||
            priceText.Contains("Please", StringComparison.OrdinalIgnoreCase) ||
            priceText.Contains("demande", StringComparison.OrdinalIgnoreCase) ||
            priceText.Contains("Contactez", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return base.ParsePrice(priceText);
    }

    protected override int? ParseMileage(string detailsText)
    {
        if (string.IsNullOrWhiteSpace(detailsText))
        {
            return null;
        }

        // Kijiji shows mileage in km format like "100,000 km" - try to extract from multi-word text
        var cleanMileage = detailsText
            .Replace(",", string.Empty)
            .Replace("km", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("kilometers", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        // Try to extract just the number from the parts
        var parts = cleanMileage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (int.TryParse(part, out var mileage))
            {
                return mileage;
            }
        }

        return null;
    }
}
