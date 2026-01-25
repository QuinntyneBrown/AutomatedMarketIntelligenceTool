using Shared.Contracts.Events;

namespace Image.Core.Events;

/// <summary>
/// Event raised when an image hash has been calculated.
/// </summary>
public sealed record ImageHashCalculatedEvent : IntegrationEvent
{
    public Guid ImageId { get; init; }
    public string SourceUrl { get; init; } = string.Empty;
    public string Hash { get; init; } = string.Empty;
    public Guid? ListingId { get; init; }
}
