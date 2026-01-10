using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class CarMaxScraper : BaseScraper
{
    public override string SiteName => "CarMax.com";

    public CarMaxScraper(ILogger<CarMaxScraper> logger)
        : base(logger)
    {
    }

    protected override string BuildSearchUrl(SearchParameters parameters, int page)
    {
        var baseUrl = new StringBuilder("https://www.carmax.com/cars/all");
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
            queryParams.Append($"&yearMin={parameters.YearMin.Value}");
        }

        if (parameters.YearMax.HasValue)
        {
            queryParams.Append($"&yearMax={parameters.YearMax.Value}");
        }

        if (parameters.PriceMin.HasValue)
        {
            queryParams.Append($"&priceMin={decimal.ToInt32(parameters.PriceMin.Value)}");
        }

        if (parameters.PriceMax.HasValue)
        {
            queryParams.Append($"&priceMax={decimal.ToInt32(parameters.PriceMax.Value)}");
        }

        if (parameters.MileageMax.HasValue)
        {
            queryParams.Append($"&mileageMax={parameters.MileageMax.Value}");
        }

        if (!string.IsNullOrWhiteSpace(parameters.PostalCode))
        {
            queryParams.Append($"&zip={HttpUtility.UrlEncode(parameters.PostalCode)}");
        }

        if (parameters.RadiusKilometers.HasValue)
        {
            // CarMax uses miles, convert km to miles (1 km = 0.621371 miles)
            var miles = (int)(parameters.RadiusKilometers.Value * 0.621371);
            queryParams.Append($"&radius={miles}");
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
            await page.WaitForSelectorAsync("[data-testid='car-tile'], .car-tile, .vehicle-card", new PageWaitForSelectorOptions
            {
                Timeout = 10000
            });

            var listingElements = await page.QuerySelectorAllAsync("[data-testid='car-tile'], .car-tile, .vehicle-card");

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
            var titleElement = await element.QuerySelectorAsync("[data-testid='car-title'], .car-title, h3");
            var title = titleElement != null ? await titleElement.InnerTextAsync() : string.Empty;

            var priceElement = await element.QuerySelectorAsync("[data-testid='price'], .price");
            var priceText = priceElement != null ? await priceElement.InnerTextAsync() : "0";
            var price = ParsePrice(priceText);

            var linkElement = await element.QuerySelectorAsync("a[href*='/car/']");
            var href = linkElement != null ? await linkElement.GetAttributeAsync("href") ?? string.Empty : string.Empty;
            var listingUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? href
                : $"https://www.carmax.com{href}";

            var externalId = ExtractExternalId(listingUrl);

            var mileageElement = await element.QuerySelectorAsync("[data-testid='mileage'], .mileage, .odometer");
            var mileageText = mileageElement != null ? await mileageElement.InnerTextAsync() : string.Empty;
            var mileage = ParseMileage(mileageText);

            var locationElement = await element.QuerySelectorAsync("[data-testid='location'], .location, .store-name");
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
                Country = "US",
                Currency = "USD",
                Condition = Condition.Used,
                SellerType = SellerType.Dealer,
                SellerName = "CarMax"
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
            var nextButton = await page.QuerySelectorAsync("[data-testid='pagination-next']:not([disabled]), .pagination .next:not(.disabled)");
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
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        try
        {
            var match = Regex.Match(url, @"/car/(\d+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        catch
        {
            // ignored
        }

        return base.ExtractExternalId(url);
    }
}
