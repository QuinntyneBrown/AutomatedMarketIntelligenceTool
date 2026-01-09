namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

/// <summary>
/// Calculates string similarity using Levenshtein distance algorithm.
/// </summary>
public class LevenshteinCalculator : ILevenshteinCalculator
{
    /// <inheritdoc/>
    public decimal Calculate()
    {
        throw new NotImplementedException("Use Calculate(string, string) instead.");
    }

    /// <inheritdoc/>
    public decimal Calculate(string value1, string value2)
    {
        if (string.IsNullOrEmpty(value1) && string.IsNullOrEmpty(value2))
        {
            return 1.0m;
        }

        if (string.IsNullOrEmpty(value1) || string.IsNullOrEmpty(value2))
        {
            return 0.0m;
        }

        // Normalize strings to uppercase for case-insensitive comparison
        var str1 = value1.ToUpperInvariant();
        var str2 = value2.ToUpperInvariant();

        // Exact match
        if (str1 == str2)
        {
            return 1.0m;
        }

        var distance = ComputeLevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);

        // Convert distance to similarity score (0.0 to 1.0)
        var similarity = 1.0m - ((decimal)distance / maxLength);
        
        return Math.Max(0.0m, similarity);
    }

    private static int ComputeLevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        for (var i = 0; i <= n; i++)
        {
            d[i, 0] = i;
        }

        for (var j = 0; j <= m; j++)
        {
            d[0, j] = j;
        }

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
