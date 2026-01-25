using Deduplication.Core.Interfaces.Calculators;

namespace Deduplication.Infrastructure.Calculators;

public sealed class StringDistanceCalculator : IStringDistanceCalculator
{
    public int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target))
            return source.Length;

        source = source.ToLowerInvariant();
        target = target.ToLowerInvariant();

        var sourceLength = source.Length;
        var targetLength = target.Length;

        var matrix = new int[sourceLength + 1, targetLength + 1];

        for (var i = 0; i <= sourceLength; i++)
            matrix[i, 0] = i;

        for (var j = 0; j <= targetLength; j++)
            matrix[0, j] = j;

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[sourceLength, targetLength];
    }

    public double CalculateLevenshteinSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
            return 1.0;

        var maxLength = Math.Max(source?.Length ?? 0, target?.Length ?? 0);
        if (maxLength == 0)
            return 1.0;

        var distance = CalculateLevenshteinDistance(source ?? "", target ?? "");
        return 1.0 - (double)distance / maxLength;
    }

    public double CalculateJaroWinklerSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
            return 1.0;
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;

        source = source.ToLowerInvariant();
        target = target.ToLowerInvariant();

        var jaroSimilarity = CalculateJaroSimilarity(source, target);

        // Calculate common prefix length (up to 4 chars)
        var prefixLength = 0;
        var maxPrefix = Math.Min(4, Math.Min(source.Length, target.Length));
        for (var i = 0; i < maxPrefix; i++)
        {
            if (source[i] == target[i])
                prefixLength++;
            else
                break;
        }

        const double scalingFactor = 0.1;
        return jaroSimilarity + prefixLength * scalingFactor * (1 - jaroSimilarity);
    }

    private static double CalculateJaroSimilarity(string source, string target)
    {
        var matchWindow = Math.Max(source.Length, target.Length) / 2 - 1;
        if (matchWindow < 0) matchWindow = 0;

        var sourceMatched = new bool[source.Length];
        var targetMatched = new bool[target.Length];

        var matches = 0;
        var transpositions = 0;

        for (var i = 0; i < source.Length; i++)
        {
            var start = Math.Max(0, i - matchWindow);
            var end = Math.Min(i + matchWindow + 1, target.Length);

            for (var j = start; j < end; j++)
            {
                if (targetMatched[j] || source[i] != target[j])
                    continue;

                sourceMatched[i] = true;
                targetMatched[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
            return 0.0;

        var k = 0;
        for (var i = 0; i < source.Length; i++)
        {
            if (!sourceMatched[i])
                continue;

            while (!targetMatched[k])
                k++;

            if (source[i] != target[k])
                transpositions++;

            k++;
        }

        return (
            (double)matches / source.Length +
            (double)matches / target.Length +
            (double)(matches - transpositions / 2.0) / matches
        ) / 3.0;
    }

    public double CalculateNGramSimilarity(string source, string target, int n = 2)
    {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
            return 1.0;
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;

        source = source.ToLowerInvariant();
        target = target.ToLowerInvariant();

        var sourceNGrams = GetNGrams(source, n);
        var targetNGrams = GetNGrams(target, n);

        if (sourceNGrams.Count == 0 && targetNGrams.Count == 0)
            return 1.0;

        var intersection = sourceNGrams.Intersect(targetNGrams).Count();
        var union = sourceNGrams.Union(targetNGrams).Count();

        return union == 0 ? 0.0 : (double)intersection / union;
    }

    private static HashSet<string> GetNGrams(string text, int n)
    {
        var ngrams = new HashSet<string>();
        if (text.Length < n)
        {
            ngrams.Add(text);
            return ngrams;
        }

        for (var i = 0; i <= text.Length - n; i++)
        {
            ngrams.Add(text.Substring(i, n));
        }

        return ngrams;
    }
}
