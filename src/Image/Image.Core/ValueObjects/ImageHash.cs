namespace Image.Core.ValueObjects;

/// <summary>
/// Represents a perceptual hash of an image.
/// </summary>
public readonly record struct ImageHash
{
    public string Value { get; init; }

    public ImageHash(string value)
    {
        Value = value ?? string.Empty;
    }

    public bool IsValid => !string.IsNullOrEmpty(Value) && Value.Length == 16;

    public double CompareTo(ImageHash other)
    {
        if (!IsValid || !other.IsValid)
            return 0;

        if (Value == other.Value)
            return 1.0;

        int differences = 0;
        int minLength = Math.Min(Value.Length, other.Value.Length);

        for (int i = 0; i < minLength; i++)
        {
            if (Value[i] != other.Value[i])
                differences++;
        }

        differences += Math.Abs(Value.Length - other.Value.Length);

        return 1.0 - (double)differences / Math.Max(Value.Length, other.Value.Length);
    }

    public override string ToString() => Value;
}
