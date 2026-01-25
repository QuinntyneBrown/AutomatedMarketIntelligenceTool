using System.Text.RegularExpressions;

namespace Shared.Contracts.ValueObjects;

/// <summary>
/// Represents a Vehicle Identification Number (VIN).
/// </summary>
public readonly partial record struct VIN
{
    private static readonly Regex VinPattern = VinRegex();

    public string Value { get; init; }

    public VIN(string value)
    {
        Value = Normalize(value);
    }

    public bool IsValid => !string.IsNullOrEmpty(Value) && VinPattern.IsMatch(Value);

    public bool IsPartial => !string.IsNullOrEmpty(Value) && Value.Length < 17;

    public static string Normalize(string? vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
            return string.Empty;

        return vin.Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
    }

    public static bool TryParse(string? value, out VIN vin)
    {
        vin = new VIN(value ?? string.Empty);
        return vin.IsValid || vin.IsPartial;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-HJ-NPR-Z0-9]{17}$", RegexOptions.Compiled)]
    private static partial Regex VinRegex();
}
