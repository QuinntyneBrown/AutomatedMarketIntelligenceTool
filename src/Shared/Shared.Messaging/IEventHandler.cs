using Shared.Contracts.Events;

namespace Shared.Messaging;

/// <summary>
/// Interface for handling integration events.
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    /// <summary>
    /// Handles the specified event.
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
