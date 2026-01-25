using Notification.Infrastructure;
using Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Notification infrastructure
var connectionString = builder.Configuration.GetConnectionString("NotificationDb")
    ?? "Server=localhost;Database=NotificationService;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddNotificationInfrastructure(connectionString);

// Add messaging (placeholder - will be configured with RabbitMQ)
builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();

// Health checks
builder.Services.AddHealthChecks();

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
app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Null event publisher for when messaging is not configured.
/// </summary>
internal sealed class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : Shared.Contracts.Events.IIntegrationEvent
    {
        return Task.CompletedTask;
    }

    public Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : Shared.Contracts.Events.IIntegrationEvent
    {
        return Task.CompletedTask;
    }
}
