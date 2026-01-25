namespace Image.Api.DTOs;

/// <summary>
/// Request to upload an image.
/// </summary>
public sealed record UploadImageRequest
{
    public IFormFile? File { get; init; }
    public string? SourceUrl { get; init; }
    public Guid? ListingId { get; init; }
}

/// <summary>
/// Request to calculate hash from URL.
/// </summary>
public sealed record HashImageRequest
{
    public required string ImageUrl { get; init; }
    public Guid? ListingId { get; init; }
}

/// <summary>
/// Request to compare two image hashes.
/// </summary>
public sealed record CompareHashesRequest
{
    public required string Hash1 { get; init; }
    public required string Hash2 { get; init; }
}

/// <summary>
/// Response with image hash result.
/// </summary>
public sealed record ImageHashResponse
{
    public Guid Id { get; init; }
    public required string SourceUrl { get; init; }
    public string? Hash { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? ListingId { get; init; }
    public DateTimeOffset ProcessedAt { get; init; }
}

/// <summary>
/// Response with hash comparison result.
/// </summary>
public sealed record HashComparisonResponse
{
    public required string Hash1 { get; init; }
    public required string Hash2 { get; init; }
    public double Similarity { get; init; }
    public bool AreSimilar { get; init; }
    public double Threshold { get; init; }
}

/// <summary>
/// Response with image details.
/// </summary>
public sealed record ImageResponse
{
    public Guid Id { get; init; }
    public required string SourceUrl { get; init; }
    public string? Hash { get; init; }
    public Guid? ListingId { get; init; }
    public long? FileSizeBytes { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public string? ContentType { get; init; }
    public string? StorageUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
    public bool IsProcessed { get; init; }
}
