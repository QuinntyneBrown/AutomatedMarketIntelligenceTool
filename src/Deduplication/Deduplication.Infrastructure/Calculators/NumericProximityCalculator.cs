using Deduplication.Core.Interfaces.Calculators;

namespace Deduplication.Infrastructure.Calculators;

public sealed class NumericProximityCalculator : INumericProximityCalculator
{
    public double CalculatePercentageSimilarity(decimal? value1, decimal? value2, decimal maxDifferencePercent = 10)
    {
        if (!value1.HasValue && !value2.HasValue)
            return 1.0;
        if (!value1.HasValue || !value2.HasValue)
            return 0.0;

        if (value1.Value == 0 && value2.Value == 0)
            return 1.0;

        var average = (value1.Value + value2.Value) / 2;
        if (average == 0)
            return 0.0;

        var difference = Math.Abs(value1.Value - value2.Value);
        var percentDifference = (difference / Math.Abs(average)) * 100;

        if (percentDifference >= maxDifferencePercent)
            return 0.0;

        return 1.0 - (double)(percentDifference / maxDifferencePercent);
    }

    public double CalculateAbsoluteSimilarity(int? value1, int? value2, int maxDifference)
    {
        if (!value1.HasValue && !value2.HasValue)
            return 1.0;
        if (!value1.HasValue || !value2.HasValue)
            return 0.0;

        var difference = Math.Abs(value1.Value - value2.Value);
        if (difference >= maxDifference)
            return 0.0;

        return 1.0 - (double)difference / maxDifference;
    }

    public double CalculatePriceSimilarity(decimal? price1, decimal? price2)
    {
        if (!price1.HasValue && !price2.HasValue)
            return 1.0;
        if (!price1.HasValue || !price2.HasValue)
            return 0.0;

        // Allow more variation for lower-priced vehicles
        var average = (price1.Value + price2.Value) / 2;
        var maxPercentDifference = average switch
        {
            < 10000 => 15m, // 15% for cheap vehicles
            < 30000 => 10m, // 10% for mid-range
            < 50000 => 7m,  // 7% for higher-end
            _ => 5m         // 5% for luxury vehicles
        };

        return CalculatePercentageSimilarity(price1, price2, maxPercentDifference);
    }

    public double CalculateMileageSimilarity(int? mileage1, int? mileage2)
    {
        if (!mileage1.HasValue && !mileage2.HasValue)
            return 1.0;
        if (!mileage1.HasValue || !mileage2.HasValue)
            return 0.0;

        // Allow reasonable mileage discrepancy (could be different units or timing of updates)
        var difference = Math.Abs(mileage1.Value - mileage2.Value);

        // Allow up to 5000 km/miles difference or 10% whichever is larger
        var maxDifference = Math.Max(5000, (int)(Math.Max(mileage1.Value, mileage2.Value) * 0.1));

        if (difference >= maxDifference)
            return 0.0;

        return 1.0 - (double)difference / maxDifference;
    }
}
