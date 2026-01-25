using ScrapingWorker.Infrastructure.Extensions;
using ScrapingWorker.Service;
using Shared.Contracts.Events;
using Shared.Messaging;
using Shared.ServiceDefaults.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add infrastructure
builder.Services.AddScrapingWorkerInfrastructure();

// Add messaging (placeholder - will be configured with RabbitMQ)
builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();

// Add worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

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
