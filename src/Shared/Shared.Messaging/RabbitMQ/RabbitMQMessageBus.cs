using System.Text;
using System.Text.Json;
using Shared.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace Shared.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of the message bus.
/// </summary>
public class RabbitMQMessageBus : IMessageBus, IDisposable
{
    private readonly RabbitMQConnection _connection;
    private readonly ILogger<RabbitMQMessageBus> _logger;
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public RabbitMQMessageBus(RabbitMQConnection connection, ILogger<RabbitMQMessageBus> logger)
    {
        _connection = connection;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var eventName = typeof(TEvent).Name;
        var message = JsonSerializer.Serialize(@event, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(message);

        await _connection.PublishAsync(eventName, body, cancellationToken);

        _logger.LogInformation("Published event {EventName} with ID {EventId}", eventName, @event.EventId);
    }

    public async Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        foreach (var @event in events)
        {
            await PublishAsync(@event, cancellationToken);
        }
    }

    public Task SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var eventType = typeof(TEvent);

        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<Delegate>();
        }

        _handlers[eventType].Add(handler);

        var eventName = eventType.Name;
        _connection.Subscribe(eventName, async (body) =>
        {
            var message = Encoding.UTF8.GetString(body);
            var @event = JsonSerializer.Deserialize<TEvent>(message, _jsonOptions);

            if (@event != null)
            {
                await handler(@event, cancellationToken);
            }
        });

        _logger.LogInformation("Subscribed to event {EventName}", eventName);

        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync<TEvent>(CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var eventType = typeof(TEvent);

        if (_handlers.ContainsKey(eventType))
        {
            _handlers.Remove(eventType);
        }

        _logger.LogInformation("Unsubscribed from event {EventName}", eventType.Name);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _handlers.Clear();
    }
}
