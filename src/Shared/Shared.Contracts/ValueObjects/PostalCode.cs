using System.Text.RegularExpressions;

namespace Shared.Contracts.ValueObjects;

/// <summary>
/// Represents a Canadian postal code.
/// </summary>
public readonly partial record struct PostalCode
{
    private static readonly Regex CanadianPostalCodePattern = CanadianPostalCodeRegex();

    public string Value { get; init; }

    public PostalCode(string value)
    {
        Value = Normalize(value);
    }

    public bool IsValid => !string.IsNullOrEmpty(Value) && CanadianPostalCodePattern.IsMatch(Value);

    /// <summary>
    /// Gets the Forward Sortation Area (first 3 characters).
    /// </summary>
    public string FSA => Value.Length >= 3 ? Value[..3] : string.Empty;

    public static string Normalize(string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            return string.Empty;

        var normalized = postalCode.Trim().ToUpperInvariant().Replace(" ", "");

        // Insert space for formatting if it's a valid 6-character code
        if (normalized.Length == 6)
            normalized = $"{normalized[..3]} {normalized[3..]}";

        return normalized;
    }

    public static bool TryParse(string? value, out PostalCode postalCode)
    {
        postalCode = new PostalCode(value ?? string.Empty);
        return postalCode.IsValid;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z]\\d[A-Z] ?\\d[A-Z]\\d$", RegexOptions.Compiled)]
    private static partial Regex CanadianPostalCodeRegex();
}
