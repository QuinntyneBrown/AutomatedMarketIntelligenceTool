using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class SearchParameters
{
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? YearMin { get; set; }
    public int? YearMax { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public int? MileageMax { get; set; }
    public string? PostalCode { get; set; }
    public int? RadiusKilometers { get; set; }
    public CanadianProvince? Province { get; set; }
    public int? MaxPages { get; set; }
    public bool HeadedMode { get; set; } = false;
}
