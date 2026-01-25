using ScrapingOrchestration.Infrastructure.Extensions;
using Shared.Contracts.Events;
using Shared.Messaging;
using Shared.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add ScrapingOrchestration infrastructure
var connectionString = builder.Configuration.GetConnectionString("ScrapingDb")
    ?? "Server=localhost;Database=ScrapingOrchestrationService;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddScrapingOrchestrationInfrastructure(connectionString);

// Add messaging (placeholder - will be configured with RabbitMQ)
builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
