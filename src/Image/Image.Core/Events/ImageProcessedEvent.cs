using Shared.Contracts.Events;

namespace Image.Core.Events;

/// <summary>
/// Event raised when an image has been processed.
/// </summary>
public sealed record ImageProcessedEvent : IntegrationEvent
{
    public Guid ImageId { get; init; }
    public string SourceUrl { get; init; } = string.Empty;
    public string? Hash { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
