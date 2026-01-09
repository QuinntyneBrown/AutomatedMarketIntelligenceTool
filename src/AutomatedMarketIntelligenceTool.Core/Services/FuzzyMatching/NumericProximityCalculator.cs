namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

/// <summary>
/// Calculates numeric similarity based on proximity with tolerance.
/// </summary>
public class NumericProximityCalculator : INumericProximityCalculator
{
    /// <inheritdoc/>
    public decimal Calculate()
    {
        throw new NotImplementedException("Use Calculate(decimal, decimal, decimal) instead.");
    }

    /// <inheritdoc/>
    public decimal Calculate(decimal value1, decimal value2, decimal tolerance)
    {
        if (tolerance <= 0)
        {
            throw new ArgumentException("Tolerance must be greater than zero.", nameof(tolerance));
        }

        var difference = Math.Abs(value1 - value2);

        // Within tolerance - perfect match
        if (difference <= tolerance)
        {
            return 1.0m;
        }

        // Calculate similarity based on distance beyond tolerance
        // Score decreases linearly as difference increases
        // At 2x tolerance, score is 0.5; at 3x tolerance, score is 0.33, etc.
        var similarity = tolerance / difference;
        
        return Math.Min(1.0m, similarity);
    }

    /// <summary>
    /// Calculates similarity for integer year values with special handling.
    /// </summary>
    /// <param name="year1">First year value.</param>
    /// <param name="year2">Second year value.</param>
    /// <param name="toleranceYears">Maximum year difference for perfect match (default 1).</param>
    /// <returns>Similarity score between 0.0 and 1.0.</returns>
    public decimal CalculateYearSimilarity(int year1, int year2, int toleranceYears = 1)
    {
        var difference = Math.Abs(year1 - year2);

        if (difference == 0)
        {
            return 1.0m;
        }

        if (difference <= toleranceYears)
        {
            return 0.8m; // ±1 year gets 0.8 as per specs
        }

        // Decrease similarity for larger differences
        // For 2 years difference: 0.7, for 3 years: 0.6, etc.
        var similarity = 0.9m - (difference * 0.1m);
        
        return Math.Max(0.0m, similarity);
    }
}
