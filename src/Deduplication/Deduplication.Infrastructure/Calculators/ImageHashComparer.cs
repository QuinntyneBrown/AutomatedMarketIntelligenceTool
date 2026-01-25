using Deduplication.Core.Interfaces.Calculators;

namespace Deduplication.Infrastructure.Calculators;

public sealed class ImageHashComparer : IImageHashComparer
{
    public int CalculateHammingDistance(string hash1, string hash2)
    {
        if (string.IsNullOrEmpty(hash1) || string.IsNullOrEmpty(hash2))
            return int.MaxValue;

        if (hash1.Length != hash2.Length)
            return int.MaxValue;

        var distance = 0;
        for (var i = 0; i < hash1.Length; i++)
        {
            if (hash1[i] != hash2[i])
                distance++;
        }

        return distance;
    }

    public double CalculateHashSimilarity(string? hash1, string? hash2)
    {
        if (string.IsNullOrEmpty(hash1) && string.IsNullOrEmpty(hash2))
            return 0.5; // Neutral when no hashes available

        if (string.IsNullOrEmpty(hash1) || string.IsNullOrEmpty(hash2))
            return 0.3; // Slight penalty when one is missing

        if (hash1.Length != hash2.Length)
            return 0.0;

        var distance = CalculateHammingDistance(hash1, hash2);
        var maxDistance = hash1.Length;

        return 1.0 - (double)distance / maxDistance;
    }

    public bool AreImagesSimilar(string? hash1, string? hash2, int maxHammingDistance = 10)
    {
        if (string.IsNullOrEmpty(hash1) || string.IsNullOrEmpty(hash2))
            return false;

        var distance = CalculateHammingDistance(hash1, hash2);
        return distance <= maxHammingDistance;
    }
}
