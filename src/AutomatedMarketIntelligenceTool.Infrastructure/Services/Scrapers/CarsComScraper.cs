using System.Text;
using System.Web;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class CarsComScraper : BaseScraper
{
    public override string SiteName => "Cars.com";

    public CarsComScraper(ILogger<CarsComScraper> logger)
        : base(logger)
    {
    }

    protected override string BuildSearchUrl(SearchParameters parameters, int page)
    {
        var baseUrl = "https://www.cars.com/shopping/results/";
        var queryParams = new StringBuilder();

        if (!string.IsNullOrEmpty(parameters.Make))
        {
            queryParams.Append($"&makes[]={HttpUtility.UrlEncode(parameters.Make.ToLower())}");
        }

        if (!string.IsNullOrEmpty(parameters.Model))
        {
            queryParams.Append($"&models[]={HttpUtility.UrlEncode(parameters.Model.ToLower())}");
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
            queryParams.Append($"&price_min={parameters.PriceMin.Value}");
        }

        if (parameters.PriceMax.HasValue)
        {
            queryParams.Append($"&price_max={parameters.PriceMax.Value}");
        }

        if (parameters.MileageMax.HasValue)
        {
            queryParams.Append($"&maximum_distance={parameters.MileageMax.Value}");
        }

        if (!string.IsNullOrEmpty(parameters.ZipCode))
        {
            queryParams.Append($"&zip={parameters.ZipCode}");

            if (parameters.RadiusMiles.HasValue)
            {
                queryParams.Append($"&radius={parameters.RadiusMiles.Value}");
            }
        }

        if (page > 1)
        {
            queryParams.Append($"&page={page}");
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
            await page.WaitForSelectorAsync(".vehicle-card", new PageWaitForSelectorOptions
            {
                Timeout = 10000
            });

            var listingElements = await page.QuerySelectorAllAsync(".vehicle-card");

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
            var titleElement = await element.QuerySelectorAsync("h2.title");
            var title = titleElement != null ? await titleElement.InnerTextAsync() : string.Empty;

            var priceElement = await element.QuerySelectorAsync(".primary-price");
            var priceText = priceElement != null ? await priceElement.InnerTextAsync() : "0";
            var price = ParsePrice(priceText);

            var linkElement = await element.QuerySelectorAsync("a.vehicle-card-link");
            var href = linkElement != null ? await linkElement.GetAttributeAsync("href") ?? string.Empty : string.Empty;
            var listingUrl = href.StartsWith("http") ? href : $"https://www.cars.com{href}";

            var externalId = ExtractExternalId(listingUrl);

            var mileageElement = await element.QuerySelectorAsync(".mileage");
            var mileageText = mileageElement != null ? await mileageElement.InnerTextAsync() : string.Empty;
            var mileage = ParseMileage(mileageText);

            var locationElement = await element.QuerySelectorAsync(".miles-from");
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
            var nextButton = await page.QuerySelectorAsync("a[aria-label='Next Page']");
            if (nextButton == null)
            {
                return false;
            }

            var isDisabled = await nextButton.GetAttributeAsync("aria-disabled");
            return isDisabled != "true";
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
            if (parts[i] == "vehicledetail" && i + 1 < parts.Length)
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
