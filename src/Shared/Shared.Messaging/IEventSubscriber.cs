using Shared.Contracts.Events;

namespace Shared.Messaging;

/// <summary>
/// Interface for subscribing to integration events.
/// </summary>
public interface IEventSubscriber
{
    /// <summary>
    /// Subscribes to an integration event type.
    /// </summary>
    Task SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;

    /// <summary>
    /// Unsubscribes from an integration event type.
    /// </summary>
    Task UnsubscribeAsync<TEvent>(CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
