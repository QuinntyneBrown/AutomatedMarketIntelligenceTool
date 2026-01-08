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
    public string? ZipCode { get; set; }
    public int? RadiusMiles { get; set; }
    public int? MaxPages { get; set; }
}
