using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class CarGurusScraper : BaseScraper
{
    public override string SiteName => "CarGurus";

    public CarGurusScraper(ILogger<CarGurusScraper> logger)
        : base(logger)
    {
    }

    protected override string BuildSearchUrl(SearchParameters parameters, int page)
    {
        // CarGurus.ca uses a different URL structure
        // Example: https://www.cargurus.ca/Cars/inventorylisting/viewDetailsFilterViewInventoryListing.action?zip=M5V&distance=25&inventorySearchWidgetType=AUTO
        var baseUrl = "https://www.cargurus.ca/Cars/inventorylisting/viewDetailsFilterViewInventoryListing.action";
        var queryParams = new StringBuilder();

        queryParams.Append("?inventorySearchWidgetType=AUTO");

        if (!string.IsNullOrWhiteSpace(parameters.PostalCode))
        {
            queryParams.Append($"&zip={HttpUtility.UrlEncode(parameters.PostalCode)}");
        }

        if (parameters.RadiusKilometers.HasValue)
        {
            queryParams.Append($"&distance={parameters.RadiusKilometers.Value}");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Make))
        {
            queryParams.Append($"&makeName={HttpUtility.UrlEncode(parameters.Make)}");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Model))
        {
            queryParams.Append($"&modelName={HttpUtility.UrlEncode(parameters.Model)}");
        }

        if (parameters.YearMin.HasValue)
        {
            queryParams.Append($"&minYear={parameters.YearMin.Value}");
        }

        if (parameters.YearMax.HasValue)
        {
            queryParams.Append($"&maxYear={parameters.YearMax.Value}");
        }

        if (parameters.PriceMin.HasValue)
        {
            queryParams.Append($"&minPrice={decimal.ToInt32(parameters.PriceMin.Value)}");
        }

        if (parameters.PriceMax.HasValue)
        {
            queryParams.Append($"&maxPrice={decimal.ToInt32(parameters.PriceMax.Value)}");
        }

        if (parameters.MileageMax.HasValue)
        {
            // CarGurus uses miles, convert from km (divide by 1.60934)
            var maxMiles = (int)(parameters.MileageMax.Value / 1.60934);
            queryParams.Append($"&maxMileage={maxMiles}");
        }

        // Pagination for CarGurus
        if (page > 1)
        {
            var offset = (page - 1) * 20; // CarGurus typically shows 20 results per page
            queryParams.Append($"&offset={offset}");
        }

        return baseUrl + queryParams.ToString();
    }

    protected override async Task<IEnumerable<ScrapedListing>> ParseListingsAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        var listings = new List<ScrapedListing>();

        try
        {
            // Wait for CarGurus listings to load
            await page.WaitForSelectorAsync("[data-cg-ft='car-blade-link']", new PageWaitForSelectorOptions
            {
                Timeout = 10000
            });

            var listingElements = await page.QuerySelectorAllAsync("[data-cg-ft='car-blade-link']");

            _logger.LogDebug("Found {Count} listing elements on CarGurus page", listingElements.Count);

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
                    _logger.LogWarning(ex, "Failed to parse individual CarGurus listing element");
                }
            }
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout waiting for CarGurus listing elements, page may be empty");
        }

        return listings;
    }

    private async Task<ScrapedListing?> ParseListingElementAsync(
        IElementHandle element,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the listing URL
            var href = await element.GetAttributeAsync("href") ?? string.Empty;
            var listingUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? href
                : $"https://www.cargurus.ca{href}";

            // Extract listing ID from URL
            var externalId = ExtractExternalId(listingUrl);

            // Get the parent container for the listing
            var container = await element.QuerySelectorAsync("..") ?? element;

            // Parse title (usually contains year, make, model)
            var titleElement = await container.QuerySelectorAsync("[data-cg-ft='car-blade-title']");
            var title = titleElement != null ? await titleElement.InnerTextAsync() : string.Empty;

            // Parse price
            var priceElement = await container.QuerySelectorAsync("[data-cg-ft='car-blade-price']");
            var priceText = priceElement != null ? await priceElement.InnerTextAsync() : "0";
            var price = ParsePrice(priceText);

            // Parse mileage
            var mileageElement = await container.QuerySelectorAsync("[data-cg-ft='car-blade-mileage']");
            var mileageText = mileageElement != null ? await mileageElement.InnerTextAsync() : string.Empty;
            var mileage = ParseMileageFromCarGurus(mileageText);

            // Parse location
            var locationElement = await container.QuerySelectorAsync("[data-cg-ft='car-blade-location']");
            var locationText = locationElement != null ? await locationElement.InnerTextAsync() : string.Empty;
            var (city, province) = ParseLocation(locationText);

            // Parse title to extract make, model, year
            var (make, model, year) = ParseTitle(title);

            if (string.IsNullOrEmpty(make) || string.IsNullOrEmpty(model) || year == 0)
            {
                _logger.LogWarning("Failed to parse required fields from CarGurus title: {Title}", title);
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
                Condition = Condition.Used // CarGurus mainly has used cars
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing CarGurus listing element");
            return null;
        }
    }

    protected override async Task<bool> HasNextPageAsync(IPage page, CancellationToken cancellationToken)
    {
        try
        {
            // Check if there's a "Next" button or more results
            var nextButton = await page.QuerySelectorAsync("a[aria-label='Next']");
            if (nextButton != null)
            {
                var isDisabled = await nextButton.GetAttributeAsync("disabled");
                return isDisabled == null;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for next page on CarGurus");
            return false;
        }
    }

    private static string ExtractExternalId(string url)
    {
        // CarGurus URLs typically contain listingId parameter
        var match = Regex.Match(url, @"listingId=(\d+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // Fallback: use the full URL as ID
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(url)).Substring(0, 32);
    }

    private static decimal ParsePrice(string priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText))
        {
            return 0;
        }

        // Remove currency symbols, commas, and whitespace
        var cleaned = Regex.Replace(priceText, @"[^\d.]", string.Empty);
        
        if (decimal.TryParse(cleaned, out var price))
        {
            return price;
        }

        return 0;
    }

    private static int? ParseMileageFromCarGurus(string mileageText)
    {
        if (string.IsNullOrWhiteSpace(mileageText))
        {
            return null;
        }

        // CarGurus might show mileage in miles or km
        // Extract just the number
        var match = Regex.Match(mileageText, @"([\d,]+)\s*(km|mi|miles|kilometers)?", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var numberText = match.Groups[1].Value.Replace(",", string.Empty);
            if (int.TryParse(numberText, out var mileage))
            {
                // If it's in miles, convert to km
                var unit = match.Groups[2].Value.ToLowerInvariant();
                if (unit == "mi" || unit == "miles")
                {
                    return (int)(mileage * 1.60934);
                }

                return mileage;
            }
        }

        return null;
    }

    private static (string? city, string? province) ParseLocation(string locationText)
    {
        if (string.IsNullOrWhiteSpace(locationText))
        {
            return (null, null);
        }

        // Location format is typically "City, Province" or "City, AB"
        var parts = locationText.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            return (parts[0], parts[1]);
        }

        return (locationText, null);
    }

    private static (string make, string model, int year) ParseTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (string.Empty, string.Empty, 0);
        }

        // CarGurus titles typically: "Year Make Model Trim"
        // Example: "2023 Toyota Camry SE"
        var match = Regex.Match(title, @"^(\d{4})\s+([A-Za-z\-]+)\s+([A-Za-z0-9\-\s]+?)(?:\s+\w{1,3})?$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var year = int.Parse(match.Groups[1].Value);
            var make = match.Groups[2].Value.Trim();
            var model = match.Groups[3].Value.Trim();
            return (make, model, year);
        }

        // Fallback parsing
        var parts = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
        {
            if (int.TryParse(parts[0], out var year))
            {
                var make = parts[1];
                var model = string.Join(" ", parts.Skip(2).Take(2)); // Take next 2 words as model
                return (make, model, year);
            }
        }

        return (string.Empty, string.Empty, 0);
    }
}
