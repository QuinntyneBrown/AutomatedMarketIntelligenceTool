namespace ScrapingWorker.Core.Models;

/// <summary>
/// Represents the result of a scraping operation.
/// </summary>
public sealed record ScrapeResult
{
    public bool Success { get; init; }
    public IReadOnlyList<ScrapedListing> Listings { get; init; } = [];
    public int TotalListingsFound { get; init; }
    public int PagesCrawled { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public static ScrapeResult Successful(
        IReadOnlyList<ScrapedListing> listings,
        int pagesCrawled,
        TimeSpan duration)
    {
        return new ScrapeResult
        {
            Success = true,
            Listings = listings,
            TotalListingsFound = listings.Count,
            PagesCrawled = pagesCrawled,
            Duration = duration
        };
    }

    public static ScrapeResult Failed(string errorMessage, TimeSpan duration)
    {
        return new ScrapeResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Duration = duration
        };
    }
}
