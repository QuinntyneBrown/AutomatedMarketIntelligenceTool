namespace ScrapingWorker.Core.Models;

/// <summary>
/// Represents the progress of a scraping operation.
/// </summary>
public sealed record ScrapeProgress
{
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int ListingsScraped { get; init; }
    public int ListingsTotal { get; init; }
    public TimeSpan Elapsed { get; init; }
    public string? CurrentUrl { get; init; }
    public string? Status { get; init; }
}
