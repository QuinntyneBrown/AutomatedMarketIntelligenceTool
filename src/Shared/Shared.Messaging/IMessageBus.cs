using Shared.Contracts.Events;

namespace Shared.Messaging;

/// <summary>
/// Message bus abstraction for publishing and subscribing to events.
/// </summary>
public interface IMessageBus : IEventPublisher, IEventSubscriber
{
}
