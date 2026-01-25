namespace Deduplication.Core.Interfaces.Calculators;

/// <summary>
/// Calculator for image hash comparison.
/// </summary>
public interface IImageHashComparer
{
    /// <summary>
    /// Calculates the Hamming distance between two hashes.
    /// </summary>
    int CalculateHammingDistance(string hash1, string hash2);

    /// <summary>
    /// Calculates similarity (0-1) based on Hamming distance.
    /// </summary>
    double CalculateHashSimilarity(string? hash1, string? hash2);

    /// <summary>
    /// Determines if two image hashes are likely duplicates.
    /// </summary>
    bool AreImagesSimilar(string? hash1, string? hash2, int maxHammingDistance = 10);
}
