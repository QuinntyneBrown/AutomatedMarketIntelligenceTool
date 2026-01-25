namespace Shared.Contracts.Events;

/// <summary>
/// Base interface for domain events that occur within a single service boundary.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }
}
