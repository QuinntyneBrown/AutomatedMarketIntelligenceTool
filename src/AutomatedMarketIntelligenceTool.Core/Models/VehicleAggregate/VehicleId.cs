namespace AutomatedMarketIntelligenceTool.Core.Models.VehicleAggregate;

/// <summary>
/// Strongly-typed identifier for Vehicle entity.
/// </summary>
public record VehicleId
{
    public Guid Value { get; }

    public VehicleId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Vehicle ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static VehicleId Create() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
