using ScrapingOrchestration.Infrastructure.Extensions;
using Shared.Contracts.Events;
using Shared.Messaging;
using Shared.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger disabled due to .NET 10.0 incompatibility
// builder.Services.AddSwaggerGen();

// Add ScrapingOrchestration infrastructure
var useInMemoryDb = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("ScrapingDb");

if (useInMemoryDb || string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddScrapingOrchestrationInfrastructureInMemory();
}
else
{
    builder.Services.AddScrapingOrchestrationInfrastructure(connectionString);
}

// Add messaging (placeholder - will be configured with RabbitMQ)
builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();

var app = builder.Build();

// Configure pipeline
// Swagger disabled due to .NET 10.0 incompatibility
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

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
