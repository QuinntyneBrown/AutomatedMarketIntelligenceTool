using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;

namespace AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;

public class ReportData
{
    public string Title { get; set; } = "Market Intelligence Report";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? SearchCriteria { get; set; }
    public MarketStatistics? Statistics { get; set; }
    public List<Listing> Listings { get; set; } = new();
}
