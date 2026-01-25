using Alert.Infrastructure;
using Shared.Contracts.Events;
using Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("AlertDb")
    ?? "Server=localhost;Database=AlertDb;Trusted_Connection=True;TrustServerCertificate=True";
builder.Services.AddAlertInfrastructure(connectionString);
builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

public sealed class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IIntegrationEvent => Task.CompletedTask;
    public Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default) where TEvent : IIntegrationEvent => Task.CompletedTask;
}
