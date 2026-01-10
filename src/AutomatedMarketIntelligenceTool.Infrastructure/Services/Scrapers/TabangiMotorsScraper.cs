using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class TabangiMotorsScraper : BaseScraper
{
    public override string SiteName => "TabangiMotors.com";

    public TabangiMotorsScraper(ILogger<TabangiMotorsScraper> logger)
        : base(logger)
    {
    }

    protected override string BuildSearchUrl(SearchParameters parameters, int page)
    {
        var baseUrl = new StringBuilder("https://www.tabangimotors.com/vehicles/used/");
        var queryParams = new List<string>();

        if (page > 1)
        {
            queryParams.Add($"pg={page}");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Make))
        {
            queryParams.Add($"mk={HttpUtility.UrlEncode(parameters.Make)}");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Model))
        {
            queryParams.Add($"md={HttpUtility.UrlEncode(parameters.Model)}");
        }

        if (parameters.YearMin.HasValue)
        {
            queryParams.Add($"ymin={parameters.YearMin.Value}");
        }

        if (parameters.YearMax.HasValue)
        {
            queryParams.Add($"ymax={parameters.YearMax.Value}");
        }

        if (parameters.PriceMin.HasValue)
        {
            queryParams.Add($"pmin={decimal.ToInt32(parameters.PriceMin.Value)}");
        }

        if (parameters.PriceMax.HasValue)
        {
            queryParams.Add($"pmax={decimal.ToInt32(parameters.PriceMax.Value)}");
        }

        if (parameters.MileageMax.HasValue)
        {
            queryParams.Add($"omax={parameters.MileageMax.Value}");
        }

        if (queryParams.Count > 0)
        {
            baseUrl.Append('?');
            baseUrl.Append(string.Join("&", queryParams));
        }

        return baseUrl.ToString();
    }

    protected override async Task<IEnumerable<ScrapedListing>> ParseListingsAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        var listings = new List<ScrapedListing>();

        try
        {
            // Log the page URL and title for debugging
            _logger.LogInformation("Current page URL: {Url}", page.Url);
            _logger.LogInformation("Current page title: {Title}", await page.TitleAsync());

            // This site uses JavaScript to load vehicles dynamically after the initial page load
            // Vehicles load approximately 5+ seconds after page load
            _logger.LogInformation("Waiting for JavaScript to load vehicles dynamically (this can take 5-10 seconds)...");

            // Try multiple selector strategies - this site appears to use Convertus theme
            var selectorStrategies = new[]
            {
                ".card__content", // Convertus theme vehicle cards
                ".inventory__grid article",
                "[class*='card__']",
                ".vehicle-card",
                ".inventory-item",
                "[data-vehicle-id]",
                ".vehicle-listing",
                ".car-card",
                "article[class*='card']",
                ".inventory-list article",
                ".search-result",
                ".vehicle-item",
                "div[class*='vehicle']",
                "div[class*='inventory']"
            };

            IReadOnlyList<IElementHandle> listingElements = Array.Empty<IElementHandle>();
            string? successfulSelector = null;

            // Wait for vehicles to load - try each selector with a longer timeout
            // Since vehicles load 5+ seconds after page load, we need to wait patiently
            foreach (var selector in selectorStrategies)
            {
                try
                {
                    _logger.LogDebug("Trying selector: {Selector}", selector);

                    // Wait up to 15 seconds for vehicles to appear (5 seconds for load + buffer)
                    await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                    {
                        Timeout = 15000
                    });

                    var elements = await page.QuerySelectorAllAsync(selector);
                    if (elements.Count > 0)
                    {
                        listingElements = elements;
                        successfulSelector = selector;
                        _logger.LogInformation("Found {Count} listing elements using selector: {Selector}", elements.Count, selector);
                        break;
                    }
                }
                catch (TimeoutException)
                {
                    // Try next selector
                    _logger.LogDebug("Selector {Selector} timed out, trying next", selector);
                }
            }

            if (listingElements.Count == 0)
            {
                _logger.LogWarning("No listing elements found with any selector strategy");

                // Save full page HTML for debugging
                try
                {
                    var fullHtml = await page.ContentAsync();
                    var debugPath = Path.Combine(Path.GetTempPath(), "tabangimotors_debug.html");
                    await File.WriteAllTextAsync(debugPath, fullHtml);
                    _logger.LogWarning("Saved page HTML to: {Path}", debugPath);

                    // Also take a screenshot
                    var screenshotPath = Path.Combine(Path.GetTempPath(), "tabangimotors_debug.png");
                    await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
                    _logger.LogWarning("Saved screenshot to: {Path}", screenshotPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save debug files");
                }

                // Log page body for debugging
                var bodyHtml = await page.InnerHTMLAsync("body");
                _logger.LogDebug("Page body (first 1000 chars): {BodyHtml}", bodyHtml.Substring(0, Math.Min(1000, bodyHtml.Length)));
                return listings;
            }

            _logger.LogDebug("Found {Count} listing elements on page using selector: {Selector}", listingElements.Count, successfulSelector);

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
            // Extract title (typically contains Year Make Model)
            var titleElement = await element.QuerySelectorAsync(
                ".vehicle-title, .vehicle-name, .car-title, h2, h3, .title a");
            var title = titleElement != null ? await titleElement.InnerTextAsync() : string.Empty;

            // Extract price
            var priceElement = await element.QuerySelectorAsync(
                ".vehicle-price, .price, .car-price, .listing-price, [class*='price']");
            var priceText = priceElement != null ? await priceElement.InnerTextAsync() : "0";
            var price = ParsePrice(priceText);

            // Extract listing URL
            var linkElement = await element.QuerySelectorAsync(
                "a[href*='/vehicles/'], a[href*='/vehicle/'], a[href*='/inventory/'], a.vehicle-link");
            var href = linkElement != null
                ? await linkElement.GetAttributeAsync("href") ?? string.Empty
                : string.Empty;

            // Try to get href from the element itself if it's a link
            if (string.IsNullOrEmpty(href))
            {
                var elementHref = await element.QuerySelectorAsync("a");
                if (elementHref != null)
                {
                    href = await elementHref.GetAttributeAsync("href") ?? string.Empty;
                }
            }

            var listingUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? href
                : $"https://www.tabangimotors.com{href}";

            var externalId = ExtractExternalId(href);

            // Extract mileage/odometer
            var mileageElement = await element.QuerySelectorAsync(
                ".mileage, .odometer, .kilometers, [class*='mileage'], [class*='odometer'], .km");
            var mileageText = mileageElement != null ? await mileageElement.InnerTextAsync() : string.Empty;
            var mileage = ParseMileage(mileageText);

            // Extract additional details if available
            var trimElement = await element.QuerySelectorAsync(".trim, .vehicle-trim");
            var trim = trimElement != null ? (await trimElement.InnerTextAsync()).Trim() : null;

            var transmissionElement = await element.QuerySelectorAsync(
                ".transmission, [class*='transmission']");
            var transmissionText = transmissionElement != null
                ? await transmissionElement.InnerTextAsync()
                : string.Empty;
            var transmission = ParseTransmission(transmissionText);

            var drivetrainElement = await element.QuerySelectorAsync(
                ".drivetrain, .drive-type, [class*='drivetrain']");
            var drivetrainText = drivetrainElement != null
                ? await drivetrainElement.InnerTextAsync()
                : string.Empty;
            var drivetrain = ParseDrivetrain(drivetrainText);

            var fuelTypeElement = await element.QuerySelectorAsync(
                ".fuel-type, .fuel, [class*='fuel']");
            var fuelTypeText = fuelTypeElement != null
                ? await fuelTypeElement.InnerTextAsync()
                : string.Empty;
            var fuelType = ParseFuelType(fuelTypeText);

            var colorElement = await element.QuerySelectorAsync(
                ".color, .exterior-color, [class*='color']");
            var colorText = colorElement != null
                ? (await colorElement.InnerTextAsync()).Trim()
                : null;

            // Extract image URLs
            var imageUrls = new List<string>();
            var imageElements = await element.QuerySelectorAllAsync("img");
            foreach (var img in imageElements)
            {
                var src = await img.GetAttributeAsync("src");
                var dataSrc = await img.GetAttributeAsync("data-src");
                var actualSrc = !string.IsNullOrWhiteSpace(dataSrc) ? dataSrc : src;

                if (!string.IsNullOrWhiteSpace(actualSrc) && !actualSrc.Contains("placeholder"))
                {
                    imageUrls.Add(actualSrc.StartsWith("http")
                        ? actualSrc
                        : $"https://www.tabangimotors.com{actualSrc}");
                }
            }

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
                Trim = trim,
                Price = price,
                Mileage = mileage,
                City = "Mississauga",
                Province = "ON",
                Country = "CA",
                Currency = "CAD",
                Condition = Condition.Used,
                Transmission = transmission,
                Drivetrain = drivetrain,
                FuelType = fuelType,
                ExteriorColor = colorText,
                SellerType = SellerType.Dealer,
                SellerName = "Tabangi Motors",
                ImageUrls = imageUrls
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
            // Check for next page button or pagination link
            var nextButton = await page.QuerySelectorAsync(
                ".pagination .next:not(.disabled), " +
                "a[rel='next'], " +
                ".pagination-next:not(.disabled), " +
                "[aria-label='Next'], " +
                ".next-page:not(.disabled), " +
                "a:has-text('Next'):not(.disabled)");

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
            return Guid.NewGuid().ToString("N")[..12];
        }

        try
        {
            // Try to extract vehicle ID from URL patterns like /vehicles/123456 or /vehicle/abc-123
            var match = Regex.Match(url, @"/(?:vehicles?|inventory)/([^/\?]+)/?(?:\?|$)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Try to get the last path segment
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            if (uri.IsAbsoluteUri)
            {
                var lastSegment = uri.Segments
                    .Select(s => s.Trim('/'))
                    .LastOrDefault(s => !string.IsNullOrWhiteSpace(s));

                if (!string.IsNullOrWhiteSpace(lastSegment))
                {
                    return lastSegment;
                }
            }
            else
            {
                var segments = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var lastSegment = segments.LastOrDefault(s => !string.IsNullOrWhiteSpace(s));
                if (!string.IsNullOrWhiteSpace(lastSegment))
                {
                    // Remove query string if present
                    var cleanSegment = lastSegment.Split('?')[0];
                    return cleanSegment;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return url.GetHashCode().ToString();
    }

    protected override decimal ParsePrice(string priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText))
        {
            return 0;
        }

        var cleanPrice = priceText
            .Replace("$", string.Empty)
            .Replace(",", string.Empty)
            .Replace("CAD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Price:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Was:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Now:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        // Extract first number found
        var match = Regex.Match(cleanPrice, @"[\d]+(?:\.[\d]+)?");
        if (match.Success && decimal.TryParse(match.Value, out var price))
        {
            return price;
        }

        return 0;
    }

    protected override int? ParseMileage(string mileageText)
    {
        if (string.IsNullOrWhiteSpace(mileageText))
        {
            return null;
        }

        var cleanMileage = mileageText
            .Replace(",", string.Empty)
            .Replace("km", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("kilometers", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("kilometres", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("miles", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("mi", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Mileage:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Odometer:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        // Extract first number found
        var match = Regex.Match(cleanMileage, @"[\d]+");
        if (match.Success && int.TryParse(match.Value, out var mileage))
        {
            return mileage;
        }

        return null;
    }

    protected override (string Make, string Model, int Year) ParseTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (string.Empty, string.Empty, 0);
        }

        var parts = title.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            return (string.Empty, string.Empty, 0);
        }

        // Try to find year (4-digit number starting with 19 or 20)
        var yearIndex = -1;
        var year = 0;

        for (var i = 0; i < parts.Length; i++)
        {
            if (Regex.IsMatch(parts[i], @"^(19|20)\d{2}$") && int.TryParse(parts[i], out year))
            {
                yearIndex = i;
                break;
            }
        }

        if (yearIndex < 0 || year == 0)
        {
            return (string.Empty, string.Empty, 0);
        }

        // Make is typically right after year
        var makeIndex = yearIndex + 1;
        if (makeIndex >= parts.Length)
        {
            return (string.Empty, string.Empty, 0);
        }

        var make = parts[makeIndex];

        // Model is everything after make (may include trim)
        var modelParts = parts.Skip(makeIndex + 1).ToArray();
        var model = modelParts.Length > 0
            ? string.Join(" ", modelParts)
            : string.Empty;

        if (string.IsNullOrEmpty(model) && parts.Length > 2)
        {
            // Fallback: year at position 0, make at 1, model is the rest
            make = parts[1];
            model = string.Join(" ", parts.Skip(2));
        }

        return (make, model, year);
    }

    private static Transmission? ParseTransmission(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var lower = text.ToLowerInvariant();

        if (lower.Contains("automatic") || lower.Contains("auto") || lower.Contains("a/t"))
        {
            return Transmission.Automatic;
        }

        if (lower.Contains("manual") || lower.Contains("stick") || lower.Contains("m/t"))
        {
            return Transmission.Manual;
        }

        if (lower.Contains("cvt"))
        {
            return Transmission.CVT;
        }

        return null;
    }

    private static Drivetrain? ParseDrivetrain(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var lower = text.ToLowerInvariant();

        if (lower.Contains("awd") || lower.Contains("all-wheel") || lower.Contains("all wheel"))
        {
            return Drivetrain.AllWheelDrive;
        }

        if (lower.Contains("4wd") || lower.Contains("4x4") || lower.Contains("four-wheel"))
        {
            return Drivetrain.FourWheelDrive;
        }

        if (lower.Contains("fwd") || lower.Contains("front-wheel") || lower.Contains("front wheel"))
        {
            return Drivetrain.FrontWheelDrive;
        }

        if (lower.Contains("rwd") || lower.Contains("rear-wheel") || lower.Contains("rear wheel"))
        {
            return Drivetrain.RearWheelDrive;
        }

        return null;
    }

    private static FuelType? ParseFuelType(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var lower = text.ToLowerInvariant();

        if (lower.Contains("electric") || lower.Contains("ev") || lower.Contains("bev"))
        {
            return FuelType.Electric;
        }

        if (lower.Contains("hybrid") || lower.Contains("phev"))
        {
            return FuelType.Hybrid;
        }

        if (lower.Contains("diesel"))
        {
            return FuelType.Diesel;
        }

        if (lower.Contains("gas") || lower.Contains("gasoline") || lower.Contains("petrol"))
        {
            return FuelType.Gasoline;
        }

        return null;
    }
}
