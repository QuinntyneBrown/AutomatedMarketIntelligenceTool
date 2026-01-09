namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

/// <summary>
/// Base interface for calculating similarity between two values.
/// </summary>
public interface ISimilarityCalculator
{
    /// <summary>
    /// Calculates similarity score between two values.
    /// </summary>
    /// <returns>Similarity score between 0.0 (no match) and 1.0 (exact match).</returns>
    decimal Calculate();
}
