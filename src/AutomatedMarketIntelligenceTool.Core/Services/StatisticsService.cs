using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<StatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MarketStatistics> GetMarketStatisticsAsync(
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
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating market statistics for tenant {TenantId}", tenantId);

        var query = _context.Listings
            .Where(l => l.TenantId == tenantId && l.IsActive);

        // Apply filters
        if (!string.IsNullOrEmpty(make))
            query = query.Where(l => l.Make == make);
        
        if (!string.IsNullOrEmpty(model))
            query = query.Where(l => l.Model == model);
        
        if (yearMin.HasValue)
            query = query.Where(l => l.Year >= yearMin.Value);
        
        if (yearMax.HasValue)
            query = query.Where(l => l.Year <= yearMax.Value);
        
        if (priceMin.HasValue)
            query = query.Where(l => l.Price >= priceMin.Value);
        
        if (priceMax.HasValue)
            query = query.Where(l => l.Price <= priceMax.Value);
        
        if (mileageMax.HasValue)
            query = query.Where(l => l.Mileage <= mileageMax.Value);
        
        if (!string.IsNullOrEmpty(condition))
            query = query.Where(l => l.Condition.ToString() == condition);
        
        if (!string.IsNullOrEmpty(bodyStyle))
            query = query.Where(l => l.BodyStyle != null && l.BodyStyle.ToString() == bodyStyle);

        var listings = await query.ToListAsync(cancellationToken);

        var statistics = new MarketStatistics
        {
            TotalListings = listings.Count
        };

        if (listings.Any())
        {
            // Price statistics
            var prices = listings.Select(l => l.Price).OrderBy(p => p).ToList();
            statistics.AveragePrice = prices.Average();
            statistics.MedianPrice = CalculateMedian(prices);
            statistics.MinPrice = prices.Min();
            statistics.MaxPrice = prices.Max();

            // Mileage statistics
            var mileages = listings.Where(l => l.Mileage.HasValue).Select(l => l.Mileage!.Value).OrderBy(m => m).ToList();
            if (mileages.Any())
            {
                statistics.AverageMileage = (int)mileages.Average();
                statistics.MedianMileage = (int)CalculateMedian(mileages.Select(m => (decimal)m).ToList());
                statistics.MinMileage = mileages.Min();
                statistics.MaxMileage = mileages.Max();
            }

            // Days on market
            var daysOnMarket = listings.Where(l => l.DaysOnMarket.HasValue).Select(l => l.DaysOnMarket!.Value).ToList();
            if (daysOnMarket.Any())
            {
                statistics.AverageDaysOnMarket = daysOnMarket.Average();
            }

            // Count by attributes
            statistics.CountByMake = listings.GroupBy(l => l.Make)
                .ToDictionary(g => g.Key, g => g.Count());
            
            statistics.CountByModel = listings.GroupBy(l => l.Model)
                .ToDictionary(g => g.Key, g => g.Count());
            
            statistics.CountByYear = listings.GroupBy(l => l.Year)
                .ToDictionary(g => g.Key, g => g.Count());
            
            statistics.CountByBodyStyle = listings.Where(l => l.BodyStyle.HasValue)
                .GroupBy(l => l.BodyStyle!.Value.ToString())
                .ToDictionary(g => g.Key, g => g.Count());
            
            statistics.CountByCondition = listings.GroupBy(l => l.Condition.ToString())
                .ToDictionary(g => g.Key, g => g.Count());
        }

        _logger.LogInformation("Calculated statistics for {Count} listings", statistics.TotalListings);
        return statistics;
    }

    public async Task<string> CalculateDealRating(
        decimal listingPrice,
        Guid tenantId,
        string make,
        string model,
        int year,
        int? mileage = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating deal rating for {Make} {Model} {Year}", make, model, year);

        var query = _context.Listings
            .Where(l => l.TenantId == tenantId 
                && l.IsActive 
                && l.Make == make 
                && l.Model == model 
                && l.Year >= year - 1 
                && l.Year <= year + 1);

        if (mileage.HasValue)
        {
            var mileageRange = mileage.Value * 0.2m; // 20% tolerance
            query = query.Where(l => l.Mileage >= mileage.Value - mileageRange && l.Mileage <= mileage.Value + mileageRange);
        }

        var similarListings = await query.ToListAsync(cancellationToken);

        if (!similarListings.Any())
        {
            _logger.LogWarning("No similar listings found for deal rating calculation");
            return "Unknown";
        }

        var averagePrice = similarListings.Average(l => l.Price);
        var percentDifference = ((listingPrice - averagePrice) / averagePrice) * 100;

        _logger.LogInformation("Price difference from average: {Difference}%", percentDifference);

        if (percentDifference <= -15)
            return "Great";
        else if (percentDifference <= -5)
            return "Good";
        else if (percentDifference <= 5)
            return "Fair";
        else
            return "High";
    }

    public Task<decimal?> CalculatePricePerMile(decimal price, int? mileage)
    {
        if (!mileage.HasValue || mileage.Value == 0)
            return Task.FromResult<decimal?>(null);

        return Task.FromResult<decimal?>(Math.Round(price / mileage.Value, 2));
    }

    private static decimal CalculateMedian(List<decimal> sortedValues)
    {
        if (sortedValues.Count == 0)
            return 0;

        int mid = sortedValues.Count / 2;
        if (sortedValues.Count % 2 == 0)
            return (sortedValues[mid - 1] + sortedValues[mid]) / 2;
        else
            return sortedValues[mid];
    }
}
