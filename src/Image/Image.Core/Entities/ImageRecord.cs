namespace Image.Core.Entities;

/// <summary>
/// Represents a stored image record with its hash.
/// </summary>
public sealed class ImageRecord
{
    public Guid Id { get; init; }
    public string SourceUrl { get; init; } = string.Empty;
    public string Hash { get; init; } = string.Empty;
    public Guid? ListingId { get; init; }
    public long? FileSizeBytes { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public string? ContentType { get; init; }
    public string? StoragePath { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
    public bool IsProcessed { get; init; }
    public string? ErrorMessage { get; init; }
}
