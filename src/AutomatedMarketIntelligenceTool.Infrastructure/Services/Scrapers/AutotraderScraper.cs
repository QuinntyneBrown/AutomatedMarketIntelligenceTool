using System.Text;
using System.Web;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class AutotraderScraper : BaseScraper
{
    public override string SiteName => "Autotrader";

    public AutotraderScraper(ILogger<AutotraderScraper> logger)
        : base(logger)
    {
    }

    protected override string BuildSearchUrl(SearchParameters parameters, int page)
    {
        var baseUrl = "https://www.autotrader.com/cars-for-sale/all-cars";
        var queryParams = new StringBuilder();

        if (!string.IsNullOrEmpty(parameters.Make))
        {
            queryParams.Append($"&makeCodeList={HttpUtility.UrlEncode(parameters.Make.ToUpper())}");
        }

        if (!string.IsNullOrEmpty(parameters.Model))
        {
            queryParams.Append($"&modelCodeList={HttpUtility.UrlEncode(parameters.Model.ToUpper())}");
        }

        if (parameters.YearMin.HasValue)
        {
            queryParams.Append($"&startYear={parameters.YearMin.Value}");
        }

        if (parameters.YearMax.HasValue)
        {
            queryParams.Append($"&endYear={parameters.YearMax.Value}");
        }

        if (parameters.PriceMin.HasValue)
        {
            queryParams.Append($"&minPrice={parameters.PriceMin.Value}");
        }

        if (parameters.PriceMax.HasValue)
        {
            queryParams.Append($"&maxPrice={parameters.PriceMax.Value}");
        }

        if (parameters.MileageMax.HasValue)
        {
            queryParams.Append($"&maxMileage={parameters.MileageMax.Value}");
        }

        if (!string.IsNullOrEmpty(parameters.ZipCode))
        {
            queryParams.Append($"&zip={parameters.ZipCode}");

            if (parameters.RadiusMiles.HasValue)
            {
                queryParams.Append($"&searchRadius={parameters.RadiusMiles.Value}");
            }
        }

        if (page > 1)
        {
            queryParams.Append($"&firstRecord={((page - 1) * 25)}");
        }

        var url = baseUrl;
        if (queryParams.Length > 0)
        {
            url += "?" + queryParams.ToString().TrimStart('&');
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
            await page.WaitForSelectorAsync("[data-cmp='inventoryListing']", new PageWaitForSelectorOptions
            {
                Timeout = 10000
            });

            var listingElements = await page.QuerySelectorAllAsync("[data-cmp='inventoryListing']");

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
            var titleElement = await element.QuerySelectorAsync("[data-cmp='subheading']");
            var title = titleElement != null ? await titleElement.InnerTextAsync() : string.Empty;

            var priceElement = await element.QuerySelectorAsync("[data-cmp='price']");
            var priceText = priceElement != null ? await priceElement.InnerTextAsync() : "0";
            var price = ParsePrice(priceText);

            var linkElement = await element.QuerySelectorAsync("a[href*='/cars-for-sale/']");
            var href = linkElement != null ? await linkElement.GetAttributeAsync("href") ?? string.Empty : string.Empty;
            var listingUrl = href.StartsWith("http") ? href : $"https://www.autotrader.com{href}";

            var externalId = ExtractExternalId(listingUrl);

            var mileageElement = await element.QuerySelectorAsync("[data-cmp='mileage']");
            var mileageText = mileageElement != null ? await mileageElement.InnerTextAsync() : string.Empty;
            var mileage = ParseMileage(mileageText);

            var locationElement = await element.QuerySelectorAsync("[data-cmp='location']");
            var locationText = locationElement != null ? await locationElement.InnerTextAsync() : string.Empty;
            var (city, state) = ParseLocation(locationText);

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
                State = state,
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
            var nextButton = await page.QuerySelectorAsync("button[aria-label='Go to next page']");
            if (nextButton == null)
            {
                return false;
            }

            var isDisabled = await nextButton.GetAttributeAsync("disabled");
            return isDisabled == null;
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
            if (parts[i] == "vehicledetails" && i + 1 < parts.Length)
            {
                return parts[i + 1].Split('?')[0];
            }
        }

        return url.GetHashCode().ToString();
    }

    private static decimal ParsePrice(string priceText)
    {
        var cleanPrice = priceText.Replace("$", string.Empty)
            .Replace(",", string.Empty)
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
            .Replace("mi", string.Empty)
            .Replace("miles", string.Empty)
            .Trim();

        if (int.TryParse(cleanMileage, out var mileage))
        {
            return mileage;
        }

        return null;
    }

    private static (string? City, string? State) ParseLocation(string locationText)
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
