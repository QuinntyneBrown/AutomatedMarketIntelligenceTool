using Microsoft.Extensions.Logging;

namespace Shared.Messaging.RabbitMQ;

/// <summary>
/// Manages RabbitMQ connection and channel operations.
/// </summary>
public class RabbitMQConnection : IDisposable
{
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQConnection> _logger;
    private readonly Dictionary<string, List<Action<byte[]>>> _subscriptions = new();
    private bool _disposed;

    public RabbitMQConnection(RabbitMQOptions options, ILogger<RabbitMQConnection> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task PublishAsync(string exchangeName, byte[] body, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would use RabbitMQ.Client
        // For now, we'll use in-memory routing for local development
        _logger.LogDebug("Publishing message to exchange {Exchange}", exchangeName);

        if (_subscriptions.TryGetValue(exchangeName, out var handlers))
        {
            foreach (var handler in handlers)
            {
                Task.Run(() => handler(body), cancellationToken);
            }
        }

        return Task.CompletedTask;
    }

    public void Subscribe(string exchangeName, Action<byte[]> handler)
    {
        if (!_subscriptions.ContainsKey(exchangeName))
        {
            _subscriptions[exchangeName] = new List<Action<byte[]>>();
        }

        _subscriptions[exchangeName].Add(handler);
        _logger.LogDebug("Subscribed to exchange {Exchange}", exchangeName);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _subscriptions.Clear();
    }
}
