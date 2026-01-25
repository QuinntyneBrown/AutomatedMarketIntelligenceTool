namespace Deduplication.Core.Interfaces.Calculators;

/// <summary>
/// Calculator for numeric proximity/similarity.
/// </summary>
public interface INumericProximityCalculator
{
    /// <summary>
    /// Calculates similarity (0-1) based on percentage difference.
    /// </summary>
    double CalculatePercentageSimilarity(decimal? value1, decimal? value2, decimal maxDifferencePercent = 10);

    /// <summary>
    /// Calculates similarity (0-1) based on absolute difference.
    /// </summary>
    double CalculateAbsoluteSimilarity(int? value1, int? value2, int maxDifference);

    /// <summary>
    /// Calculates price similarity accounting for typical price variations.
    /// </summary>
    double CalculatePriceSimilarity(decimal? price1, decimal? price2);

    /// <summary>
    /// Calculates mileage similarity.
    /// </summary>
    double CalculateMileageSimilarity(int? mileage1, int? mileage2);
}
