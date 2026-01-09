namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

/// <summary>
/// Calculator for numeric similarity based on proximity.
/// </summary>
public interface INumericProximityCalculator : ISimilarityCalculator
{
    /// <summary>
    /// Calculates similarity between two numeric values based on tolerance.
    /// </summary>
    /// <param name="value1">First numeric value.</param>
    /// <param name="value2">Second numeric value.</param>
    /// <param name="tolerance">Maximum difference for perfect match.</param>
    /// <returns>Similarity score between 0.0 and 1.0.</returns>
    decimal Calculate(decimal value1, decimal value2, decimal tolerance);

    /// <summary>
    /// Calculates similarity for integer year values with special handling.
    /// </summary>
    /// <param name="year1">First year value.</param>
    /// <param name="year2">Second year value.</param>
    /// <param name="toleranceYears">Maximum year difference for perfect match (default 1).</param>
    /// <returns>Similarity score between 0.0 and 1.0.</returns>
    decimal CalculateYearSimilarity(int year1, int year2, int toleranceYears = 1);
}
