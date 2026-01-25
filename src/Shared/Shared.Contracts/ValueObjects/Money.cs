namespace Shared.Contracts.ValueObjects;

/// <summary>
/// Represents a monetary value with currency.
/// </summary>
public readonly record struct Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency = "CAD")
    {
        Amount = amount;
        Currency = currency ?? "CAD";
    }

    public static Money CAD(decimal amount) => new(amount, "CAD");
    public static Money USD(decimal amount) => new(amount, "USD");

    public override string ToString() => $"{Amount:N2} {Currency}";
}
