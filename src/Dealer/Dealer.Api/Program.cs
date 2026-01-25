using Shared.Contracts.Events;
using Shared.Messaging;
using Shared.ServiceDefaults.Extensions;
using Dealer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DealerDb")
    ?? "Server=localhost;Database=DealerDb;Trusted_Connection=True;TrustServerCertificate=True";
builder.Services.AddDealerInfrastructure(connectionString);
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
app.MapDefaultEndpoints();

app.Run();

public sealed class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IIntegrationEvent => Task.CompletedTask;
    public Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default) where TEvent : IIntegrationEvent => Task.CompletedTask;
}
