namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class ScrapeProgress
{
    public required string SiteName { get; init; }
    public int CurrentPage { get; init; }
    public int TotalListingsFound { get; init; }
    public string? Message { get; init; }
}
