using ScrapingWorker.Infrastructure.Extensions;
using ScrapingWorker.Service;
using Shared.Contracts.Events;
using Shared.Messaging;

var builder = Host.CreateApplicationBuilder(args);

// Add infrastructure
builder.Services.AddScrapingWorkerInfrastructure();

// Add messaging (placeholder - will be configured with RabbitMQ)
builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();

// Add worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

/// <summary>
/// Null event publisher for when messaging is not configured.
/// </summary>
internal sealed class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        return Task.CompletedTask;
    }

    public Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        return Task.CompletedTask;
    }
}
