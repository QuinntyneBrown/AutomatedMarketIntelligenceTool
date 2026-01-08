using System.Text;
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
        var baseUrl = "https://www.kijiji.ca/b-cars-vehicles/";
        var location = "canada";
        
        if (!string.IsNullOrEmpty(parameters.PostalCode))
        {
            location = parameters.PostalCode.Replace(" ", string.Empty);
        }
        else if (parameters.Province.HasValue)
        {
            location = parameters.Province.Value.ToString().ToLower();
        }

        baseUrl += $"{location}/";
        
        var queryParams = new StringBuilder();

        if (!string.IsNullOrEmpty(parameters.Make))
        {
            queryParams.Append($"&carmake={HttpUtility.UrlEncode(parameters.Make)}");
        }

        if (!string.IsNullOrEmpty(parameters.Model))
        {
            queryParams.Append($"&carmodel={HttpUtility.UrlEncode(parameters.Model)}");
        }

        if (parameters.YearMin.HasValue)
        {
            queryParams.Append($"&carypmin={parameters.YearMin.Value}");
        }

        if (parameters.YearMax.HasValue)
        {
            queryParams.Append($"&carypmax={parameters.YearMax.Value}");
        }

        if (parameters.PriceMin.HasValue)
        {
            queryParams.Append($"&pricemin={parameters.PriceMin.Value}");
        }

        if (parameters.PriceMax.HasValue)
        {
            queryParams.Append($"&pricemax={parameters.PriceMax.Value}");
        }

        if (parameters.MileageMax.HasValue)
        {
            queryParams.Append($"&carod={parameters.MileageMax.Value}");
        }

        if (parameters.RadiusKilometers.HasValue)
        {
            queryParams.Append($"&radius={parameters.RadiusKilometers.Value}");
        }

        if (page > 1)
        {
            baseUrl += $"page-{page}/";
        }

        baseUrl += "c174";

        if (queryParams.Length > 0)
        {
            baseUrl += "?" + queryParams.ToString().TrimStart('&');
        }

        return baseUrl;
    }

    protected override async Task<IEnumerable<ScrapedListing>> ParseListingsAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        var listings = new List<ScrapedListing>();

        try
        {
            await page.WaitForSelectorAsync(".search-item", new PageWaitForSelectorOptions
            {
                Timeout = 10000
            });

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

    private static string ExtractExternalId(string url)
    {
        var parts = url.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            // Kijiji URLs often have IDs starting with "v-" or numeric IDs
            if (i < parts.Length && (parts[i].StartsWith("v-") || (long.TryParse(parts[i], out _) && parts[i].Length > 5)))
            {
                return parts[i];
            }
        }

        // Fallback to hash if no valid ID found
        return url.GetHashCode().ToString();
    }

    private static decimal ParsePrice(string priceText)
    {
        var cleanPrice = priceText.Replace("$", string.Empty)
            .Replace(",", string.Empty)
            .Replace("CAD", string.Empty)
            .Trim();

        // Handle various "contact for price" phrases (English and French)
        if (cleanPrice.Contains("Contact", StringComparison.OrdinalIgnoreCase) ||
            cleanPrice.Contains("Call", StringComparison.OrdinalIgnoreCase) ||
            cleanPrice.Contains("Please", StringComparison.OrdinalIgnoreCase) ||
            cleanPrice.Contains("demande", StringComparison.OrdinalIgnoreCase) ||
            cleanPrice.Contains("Contactez", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (decimal.TryParse(cleanPrice, out var price))
        {
            return price;
        }

        return 0;
    }

    private static int? ParseMileage(string detailsText)
    {
        // Kijiji shows mileage in km format like "100,000 km"
        var cleanMileage = detailsText.Replace(",", string.Empty)
            .Replace("km", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("kilometers", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        // Try to extract just the number
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

    private static (string? City, string? Province) ParseLocation(string locationText)
    {
        var parts = locationText.Split(',');
        if (parts.Length >= 2)
        {
            return (parts[0].Trim(), parts[1].Trim());
        }

        // Sometimes just city or province
        if (parts.Length == 1 && !string.IsNullOrWhiteSpace(parts[0]))
        {
            return (parts[0].Trim(), null);
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
