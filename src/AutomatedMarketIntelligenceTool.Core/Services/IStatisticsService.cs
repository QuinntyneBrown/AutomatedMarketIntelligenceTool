namespace AutomatedMarketIntelligenceTool.Core.Services;

public class MarketStatistics
{
    public int TotalListings { get; set; }
    public decimal? AveragePrice { get; set; }
    public decimal? MedianPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? AverageMileage { get; set; }
    public int? MedianMileage { get; set; }
    public int? MinMileage { get; set; }
    public int? MaxMileage { get; set; }
    public double? AverageDaysOnMarket { get; set; }
    public Dictionary<string, int> CountByMake { get; set; } = new();
    public Dictionary<string, int> CountByModel { get; set; } = new();
    public Dictionary<int, int> CountByYear { get; set; } = new();
    public Dictionary<string, int> CountByBodyStyle { get; set; } = new();
    public Dictionary<string, int> CountByCondition { get; set; } = new();
}

public interface IStatisticsService
{
    Task<MarketStatistics> GetMarketStatisticsAsync(
        Guid tenantId,
        string? make = null,
        string? model = null,
        int? yearMin = null,
        int? yearMax = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        int? mileageMax = null,
        string? condition = null,
        string? bodyStyle = null,
        CancellationToken cancellationToken = default);

    Task<string> CalculateDealRating(
        decimal listingPrice,
        Guid tenantId,
        string make,
        string model,
        int year,
        int? mileage = null,
        CancellationToken cancellationToken = default);

    Task<decimal?> CalculatePricePerMile(
        decimal price,
        int? mileage);
}
