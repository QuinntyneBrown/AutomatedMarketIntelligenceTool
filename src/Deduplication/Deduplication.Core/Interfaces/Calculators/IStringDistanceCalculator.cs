namespace Deduplication.Core.Interfaces.Calculators;

/// <summary>
/// Calculator for string distance/similarity metrics.
/// </summary>
public interface IStringDistanceCalculator
{
    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// </summary>
    int CalculateLevenshteinDistance(string source, string target);

    /// <summary>
    /// Calculates normalized similarity (0-1) using Levenshtein distance.
    /// </summary>
    double CalculateLevenshteinSimilarity(string source, string target);

    /// <summary>
    /// Calculates Jaro-Winkler similarity (0-1).
    /// </summary>
    double CalculateJaroWinklerSimilarity(string source, string target);

    /// <summary>
    /// Calculates n-gram similarity (0-1).
    /// </summary>
    double CalculateNGramSimilarity(string source, string target, int n = 2);
}
