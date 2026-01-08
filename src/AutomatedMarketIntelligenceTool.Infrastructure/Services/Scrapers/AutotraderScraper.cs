using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class AutotraderScraper : BaseScraper
{
    public override string SiteName => "Autotrader.ca";

    public AutotraderScraper(ILogger<AutotraderScraper> logger)
        : base(logger)
    {
    }

    protected override string BuildSearchUrl(SearchParameters parameters, int page)
    {
        // Autotrader uses make/model as path segments (e.g. /cars/volkswagen/jetta/)
        // and paging via rcp/rcs query parameters.
        var baseUrl = new StringBuilder("https://www.autotrader.ca/cars");

        if (!string.IsNullOrWhiteSpace(parameters.Make))
        {
            baseUrl.Append('/');
            baseUrl.Append(EscapePathSegment(parameters.Make));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Model))
        {
            baseUrl.Append('/');
            baseUrl.Append(EscapePathSegment(parameters.Model));
        }

        baseUrl.Append('/');

        var queryParams = new StringBuilder();

        // These match the observed SRP query string in debug dumps.
        const int resultsPerPage = 15;
        queryParams.Append($"&rcp={resultsPerPage}&rcs={Math.Max(0, (page - 1) * resultsPerPage)}");
        queryParams.Append("&srt=39");
        queryParams.Append("&prx=-1");
        queryParams.Append("&hprc=True");
        queryParams.Append("&wcp=True");
        queryParams.Append("&inMarket=advancedSearch");

        if (!string.IsNullOrWhiteSpace(parameters.PostalCode))
        {
            queryParams.Append($"&loc={HttpUtility.UrlEncode(parameters.PostalCode)}");
        }

        if (parameters.RadiusKilometers.HasValue)
        {
            queryParams.Append($"&radius={parameters.RadiusKilometers.Value}");
        }

        if (parameters.YearMin.HasValue)
        {
            queryParams.Append($"&ymin={parameters.YearMin.Value}");
        }

        if (parameters.YearMax.HasValue)
        {
            queryParams.Append($"&ymax={parameters.YearMax.Value}");
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
            queryParams.Append($"&odommax={parameters.MileageMax.Value}");
        }

        // Note: Province/year/price filters vary by Autotrader search mode and can change;
        // keep this URL minimal and let parsing do the heavy lifting.

        var url = baseUrl.ToString();
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
            // Current SRP markup (seen in debug dump) uses .result-item under #SearchListings.
            await page.WaitForSelectorAsync("#SearchListings .result-item", new PageWaitForSelectorOptions
            {
                Timeout = 10000
            });

            var listingElements = await page.QuerySelectorAllAsync("#SearchListings .result-item");

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
            var titleElement = await element.QuerySelectorAsync(".result-title .title-with-trim");
            var title = titleElement != null ? await titleElement.InnerTextAsync() : string.Empty;

            var priceElement = await element.QuerySelectorAsync(".price .price-amount");
            var priceText = priceElement != null ? await priceElement.InnerTextAsync() : "0";
            var price = ParsePrice(priceText);

            var linkElement = await element.QuerySelectorAsync("a.inner-link[href^='/a/']");
            var href = linkElement != null ? await linkElement.GetAttributeAsync("href") ?? string.Empty : string.Empty;
            var listingUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? href
                : $"https://www.autotrader.ca{href}";

            var externalId = ExtractExternalId(listingUrl);

            var mileageElement = await element.QuerySelectorAsync(".odometer-proximity");
            var mileageText = mileageElement != null ? await mileageElement.InnerTextAsync() : string.Empty;
            var mileage = ParseMileage(mileageText);

            var (city, province) = TryParseCityProvinceFromListingUrl(listingUrl);

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
            var active = await page.QuerySelectorAsync(".srpPager li.page-item.active");
            var next = await page.QuerySelectorAsync(".srpPager li.last-page.page-item");

            if (active == null || next == null)
            {
                return false;
            }

            var activePageRaw = await active.GetAttributeAsync("data-page");
            var nextPageRaw = await next.GetAttributeAsync("data-page");

            if (!int.TryParse(activePageRaw, out var activePage))
            {
                return false;
            }

            if (!int.TryParse(nextPageRaw, out var nextPage))
            {
                return false;
            }

            return nextPage > activePage;
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
            .Replace("mi", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("miles", string.Empty, StringComparison.OrdinalIgnoreCase)
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

    private static string EscapePathSegment(string value)
    {
        // Uri.EscapeDataString is more appropriate for path segments than HttpUtility.UrlEncode.
        var trimmed = value.Trim().ToLowerInvariant();
        return Uri.EscapeDataString(trimmed);
    }

    private static (string? City, string? Province) TryParseCityProvinceFromListingUrl(string listingUrl)
    {
        // Example: /a/volkswagen/tiguan/vancouver/british%20columbia/5_68508418_bs2006103195533/
        try
        {
            var uri = new Uri(listingUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 6 && string.Equals(segments[0], "a", StringComparison.OrdinalIgnoreCase))
            {
                var city = Uri.UnescapeDataString(segments[3]).Replace('+', ' ').Trim();
                var province = Uri.UnescapeDataString(segments[4]).Replace('+', ' ').Trim();
                return (string.IsNullOrWhiteSpace(city) ? null : city, string.IsNullOrWhiteSpace(province) ? null : province);
            }
        }
        catch
        {
            // ignored
        }

        return (null, null);
    }
}
