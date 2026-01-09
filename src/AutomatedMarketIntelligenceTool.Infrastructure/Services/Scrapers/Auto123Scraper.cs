using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class Auto123Scraper : BaseScraper
{
    public override string SiteName => "Auto123.com";

    public Auto123Scraper(ILogger<Auto123Scraper> logger)
        : base(logger)
    {
    }

    protected override string BuildSearchUrl(SearchParameters parameters, int page)
    {
        var baseUrl = new StringBuilder("https://www.auto123.com/en/used-cars");
        var queryParams = new StringBuilder();

        queryParams.Append($"?page={page}");

        if (!string.IsNullOrWhiteSpace(parameters.Make))
        {
            queryParams.Append($"&make={HttpUtility.UrlEncode(parameters.Make)}");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Model))
        {
            queryParams.Append($"&model={HttpUtility.UrlEncode(parameters.Model)}");
        }

        if (parameters.YearMin.HasValue)
        {
            queryParams.Append($"&year_min={parameters.YearMin.Value}");
        }

        if (parameters.YearMax.HasValue)
        {
            queryParams.Append($"&year_max={parameters.YearMax.Value}");
        }

        if (parameters.PriceMin.HasValue)
        {
            queryParams.Append($"&price_min={decimal.ToInt32(parameters.PriceMin.Value)}");
        }

        if (parameters.PriceMax.HasValue)
        {
            queryParams.Append($"&price_max={decimal.ToInt32(parameters.PriceMax.Value)}");
        }

        if (parameters.MileageMax.HasValue)
        {
            queryParams.Append($"&mileage_max={parameters.MileageMax.Value}");
        }

        if (!string.IsNullOrWhiteSpace(parameters.PostalCode))
        {
            queryParams.Append($"&postal_code={HttpUtility.UrlEncode(parameters.PostalCode)}");
        }

        if (parameters.RadiusKilometers.HasValue)
        {
            queryParams.Append($"&radius={parameters.RadiusKilometers.Value}");
        }

        return baseUrl.ToString() + queryParams.ToString();
    }

    protected override async Task<IEnumerable<ScrapedListing>> ParseListingsAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        var listings = new List<ScrapedListing>();

        try
        {
            await page.WaitForSelectorAsync(".car-item, .vehicle-card, article[data-vehicle]", new PageWaitForSelectorOptions
            {
                Timeout = 10000
            });

            var listingElements = await page.QuerySelectorAllAsync(".car-item, .vehicle-card, article[data-vehicle]");

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

    private async Task<ScrapedListing?> ParseListingElementAsync(
        IElementHandle element,
        CancellationToken cancellationToken)
    {
        try
        {
            var titleElement = await element.QuerySelectorAsync(".car-title, .vehicle-name, h3");
            var title = titleElement != null ? await titleElement.InnerTextAsync() : string.Empty;

            var priceElement = await element.QuerySelectorAsync(".price, .vehicle-price");
            var priceText = priceElement != null ? await priceElement.InnerTextAsync() : "0";
            var price = ParsePrice(priceText);

            var linkElement = await element.QuerySelectorAsync("a[href*='/used-car/'], a[href*='/vehicle/']");
            var href = linkElement != null ? await linkElement.GetAttributeAsync("href") ?? string.Empty : string.Empty;
            var listingUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? href
                : $"https://www.auto123.com{href}";

            var externalId = ExtractExternalId(listingUrl);

            var mileageElement = await element.QuerySelectorAsync(".mileage, .odometer");
            var mileageText = mileageElement != null ? await mileageElement.InnerTextAsync() : string.Empty;
            var mileage = ParseMileage(mileageText);

            var locationElement = await element.QuerySelectorAsync(".location, .dealer-location");
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
            var nextButton = await page.QuerySelectorAsync(".pagination .next:not(.disabled), a[rel='next']");
            return nextButton != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for next page");
            return false;
        }
    }

    private static string ExtractExternalId(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        try
        {
            var match = Regex.Match(url, @"/(?:used-car|vehicle)/([^/\?]+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

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

    private static decimal ParsePrice(string priceText)
    {
        var cleanPrice = priceText.Replace("$", string.Empty)
            .Replace(",", string.Empty)
            .Replace("CAD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (decimal.TryParse(cleanPrice, out var price))
        {
            return price;
        }

        return 0;
    }

    private static int? ParseMileage(string mileageText)
    {
        var cleanMileage = mileageText.Replace(",", string.Empty)
            .Replace("km", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("kilometers", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("kilometres", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (int.TryParse(cleanMileage, out var mileage))
        {
            return mileage;
        }

        return null;
    }

    private static (string? City, string? Province) ParseLocation(string locationText)
    {
        var parts = locationText.Split(',');
        if (parts.Length >= 2)
        {
            return (parts[0].Trim(), parts[1].Trim());
        }

        return (null, null);
    }

    private static (string Make, string Model, int Year) ParseTitle(string title)
    {
        var parts = title.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
        {
            return (string.Empty, string.Empty, 0);
        }

        if (int.TryParse(parts[0], out var year))
        {
            var make = parts[1];
            var model = string.Join(" ", parts.Skip(2));
            return (make, model, year);
        }

        return (string.Empty, string.Empty, 0);
    }
}
