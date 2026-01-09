namespace AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;

public class AlertCriteria
{
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? YearMin { get; set; }
    public int? YearMax { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public int? MileageMax { get; set; }
    public string? Location { get; set; }
    public string? Dealer { get; set; }
}
