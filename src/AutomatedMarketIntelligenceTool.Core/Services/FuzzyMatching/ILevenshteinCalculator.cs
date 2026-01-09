namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

/// <summary>
/// Calculator for string similarity using Levenshtein distance algorithm.
/// </summary>
public interface ILevenshteinCalculator : ISimilarityCalculator
{
    /// <summary>
    /// Calculates similarity between two strings using Levenshtein distance.
    /// </summary>
    /// <param name="value1">First string value.</param>
    /// <param name="value2">Second string value.</param>
    /// <returns>Similarity score between 0.0 and 1.0.</returns>
    decimal Calculate(string value1, string value2);
}
