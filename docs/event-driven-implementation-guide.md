# Event-Driven Modular Architecture - Implementation Guide

## Overview

This guide provides practical implementation details for building the event-driven modular system described in the architecture design document.

---

## 1. Shared Contracts Package Structure

### 1.1 Package Organization

```
src/
└── Shared/
    ├── AutomatedMarketIntelligenceTool.Shared.Abstractions/
    │   ├── Events/
    │   │   ├── IIntegrationEvent.cs
    │   │   ├── IIntegrationEventHandler.cs
    │   │   ├── IEventBus.cs
    │   │   └── EventMetadata.cs
    │   ├── Commands/
    │   │   ├── ICommand.cs
    │   │   ├── ICommandHandler.cs
    │   │   └── ICommandBus.cs
    │   ├── Queries/
    │   │   ├── IQuery.cs
    │   │   ├── IQueryHandler.cs
    │   │   └── IQueryBus.cs
    │   ├── Domain/
    │   │   ├── IAggregateRoot.cs
    │   │   ├── IDomainEvent.cs
    │   │   ├── Entity.cs
    │   │   └── ValueObject.cs
    │   ├── Contexts/
    │   │   ├── ITenantContext.cs
    │   │   └── ICorrelationContext.cs
    │   └── Modules/
    │       ├── IModule.cs
    │       └── IModuleClient.cs
    │
    ├── AutomatedMarketIntelligenceTool.Shared.Infrastructure/
    │   ├── EventBus/
    │   │   ├── RabbitMQ/
    │   │   │   ├── RabbitMQEventBus.cs
    │   │   │   ├── RabbitMQOptions.cs
    │   │   │   └── RabbitMQExtensions.cs
    │   │   ├── InMemory/
    │   │   │   └── InMemoryEventBus.cs
    │   │   └── Outbox/
    │   │       ├── OutboxMessage.cs
    │   │       ├── OutboxProcessor.cs
    │   │       └── OutboxExtensions.cs
    │   ├── Messaging/
    │   │   ├── MessageSerializer.cs
    │   │   ├── MessageEnvelope.cs
    │   │   └── MessageConventions.cs
    │   ├── Persistence/
    │   │   ├── BaseDbContext.cs
    │   │   ├── UnitOfWork.cs
    │   │   └── Extensions/
    │   ├── Logging/
    │   │   ├── SerilogExtensions.cs
    │   │   └── LoggingBehavior.cs
    │   ├── Observability/
    │   │   ├── OpenTelemetryExtensions.cs
    │   │   ├── MetricsCollector.cs
    │   │   └── TracingBehavior.cs
    │   └── Resilience/
    │       ├── CircuitBreakerPolicy.cs
    │       ├── RetryPolicy.cs
    │       └── ResilienceExtensions.cs
    │
    └── AutomatedMarketIntelligenceTool.Shared.Contracts/
        └── Common/
            ├── PagedResult.cs
            ├── ErrorResponse.cs
            └── ApiResponse.cs
```

### 1.2 Core Abstractions Implementation

```csharp
// src/Shared/AutomatedMarketIntelligenceTool.Shared.Abstractions/Events/IIntegrationEvent.cs
namespace AutomatedMarketIntelligenceTool.Shared.Abstractions.Events;

/// <summary>
/// Base interface for all integration events that cross bounded context boundaries.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// UTC timestamp when the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Tenant context for multi-tenancy support.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Schema version for evolution support. Defaults to 1.
    /// </summary>
    int Version => 1;
}

// src/Shared/AutomatedMarketIntelligenceTool.Shared.Abstractions/Events/IIntegrationEventHandler.cs
namespace AutomatedMarketIntelligenceTool.Shared.Abstractions.Events;

/// <summary>
/// Handler interface for processing integration events.
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    /// <summary>
    /// Handle the integration event.
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

// src/Shared/AutomatedMarketIntelligenceTool.Shared.Abstractions/Events/IEventBus.cs
namespace AutomatedMarketIntelligenceTool.Shared.Abstractions.Events;

/// <summary>
/// Event bus for publishing and subscribing to integration events.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish an integration event to the message broker.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;

    /// <summary>
    /// Publish multiple events atomically.
    /// </summary>
    Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;
}

// src/Shared/AutomatedMarketIntelligenceTool.Shared.Abstractions/Events/EventMetadata.cs
namespace AutomatedMarketIntelligenceTool.Shared.Abstractions.Events;

/// <summary>
/// Metadata attached to events for tracing and debugging.
/// </summary>
public sealed record EventMetadata(
    string CorrelationId,
    string? CausationId,
    string SourceModule,
    string? UserId,
    DateTime PublishedAt
);
```

### 1.3 Context Abstractions

```csharp
// src/Shared/AutomatedMarketIntelligenceTool.Shared.Abstractions/Contexts/ITenantContext.cs
namespace AutomatedMarketIntelligenceTool.Shared.Abstractions.Contexts;

/// <summary>
/// Provides tenant context for multi-tenancy support.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Current tenant identifier.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Tenant name for display purposes.
    /// </summary>
    string TenantName { get; }
}

// src/Shared/AutomatedMarketIntelligenceTool.Shared.Abstractions/Contexts/ICorrelationContext.cs
namespace AutomatedMarketIntelligenceTool.Shared.Abstractions.Contexts;

/// <summary>
/// Provides correlation context for distributed tracing.
/// </summary>
public interface ICorrelationContext
{
    /// <summary>
    /// Unique identifier for the current request/operation chain.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Identifier of the event/request that caused this operation.
    /// </summary>
    string? CausationId { get; }

    /// <summary>
    /// Current user identifier if authenticated.
    /// </summary>
    Guid? UserId { get; }
}
```

### 1.4 Module Abstractions

```csharp
// src/Shared/AutomatedMarketIntelligenceTool.Shared.Abstractions/Modules/IModule.cs
namespace AutomatedMarketIntelligenceTool.Shared.Abstractions.Modules;

/// <summary>
/// Interface for defining a bounded context module.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Module name for identification.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Module version following semver.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Configure services for dependency injection.
    /// </summary>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Configure HTTP endpoints.
    /// </summary>
    void ConfigureEndpoints(IEndpointRouteBuilder endpoints);

    /// <summary>
    /// Configure event subscriptions.
    /// </summary>
    void ConfigureEventHandlers(IEventBusBuilder builder);

    /// <summary>
    /// Run database migrations on startup.
    /// </summary>
    Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
```

---

## 2. Module Contract Packages

Each module publishes its own contracts package for other modules to consume.

### 2.1 Listings Module Contracts

```csharp
// src/Modules/Listings/AutomatedMarketIntelligenceTool.Listings.Contracts/Events/ListingCreatedEvent.cs
namespace AutomatedMarketIntelligenceTool.Listings.Contracts.Events;

/// <summary>
/// Published when a new listing is created in the system.
/// </summary>
public sealed record ListingCreatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ListingId,
    string Make,
    string Model,
    int Year,
    decimal Price,
    int? Mileage,
    string? Location,
    string? PostalCode,
    string SourceSite,
    string ExternalId,
    string? Vin,
    string? Condition,
    string? SellerType
) : IIntegrationEvent
{
    public int Version => 2;
}

// src/Modules/Listings/AutomatedMarketIntelligenceTool.Listings.Contracts/Events/ListingPriceChangedEvent.cs
namespace AutomatedMarketIntelligenceTool.Listings.Contracts.Events;

/// <summary>
/// Published when a listing's price changes.
/// </summary>
public sealed record ListingPriceChangedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ListingId,
    string Make,
    string Model,
    int Year,
    decimal OldPrice,
    decimal NewPrice,
    decimal ChangeAmount,
    decimal ChangePercentage
) : IIntegrationEvent;

// src/Modules/Listings/AutomatedMarketIntelligenceTool.Listings.Contracts/Events/ListingDeactivatedEvent.cs
namespace AutomatedMarketIntelligenceTool.Listings.Contracts.Events;

public enum DeactivationReason
{
    Sold,
    Expired,
    RemovedByUser,
    DuplicateDetected,
    PolicyViolation,
    Unknown
}

/// <summary>
/// Published when a listing is deactivated/removed.
/// </summary>
public sealed record ListingDeactivatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ListingId,
    DeactivationReason Reason,
    DateTime DeactivatedAt
) : IIntegrationEvent;

// src/Modules/Listings/AutomatedMarketIntelligenceTool.Listings.Contracts/Queries/GetListingQuery.cs
namespace AutomatedMarketIntelligenceTool.Listings.Contracts.Queries;

/// <summary>
/// Query to retrieve a listing by ID via the Listings module API.
/// </summary>
public sealed record GetListingQuery(Guid ListingId);

/// <summary>
/// Response DTO for listing queries.
/// </summary>
public sealed record ListingDto(
    Guid Id,
    string Make,
    string Model,
    int Year,
    decimal Price,
    int? Mileage,
    string? Location,
    string? PostalCode,
    string Condition,
    string SellerType,
    string SourceSite,
    string ExternalId,
    string? Vin,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastUpdatedAt
);
```

### 2.2 Acquisition Module Contracts

```csharp
// src/Modules/Acquisition/AutomatedMarketIntelligenceTool.Acquisition.Contracts/Events/ScrapedListingAvailableEvent.cs
namespace AutomatedMarketIntelligenceTool.Acquisition.Contracts.Events;

/// <summary>
/// Published when raw listing data has been scraped and is ready for processing.
/// </summary>
public sealed record ScrapedListingAvailableEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ScrapedListingId,
    string SourceSite,
    string ExternalId,
    string? Url,
    ScrapedListingData Data
) : IIntegrationEvent;

/// <summary>
/// Raw scraped listing data structure.
/// </summary>
public sealed record ScrapedListingData(
    string? Title,
    string? Make,
    string? Model,
    int? Year,
    decimal? Price,
    int? Mileage,
    string? Location,
    string? PostalCode,
    string? Vin,
    string? Condition,
    string? Description,
    string? SellerName,
    string? SellerType,
    List<string>? ImageUrls,
    Dictionary<string, string>? AdditionalAttributes,
    DateTime ScrapedAt
);

// src/Modules/Acquisition/AutomatedMarketIntelligenceTool.Acquisition.Contracts/Events/ScraperHealthChangedEvent.cs
namespace AutomatedMarketIntelligenceTool.Acquisition.Contracts.Events;

public enum ScraperHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Published when a scraper's health status changes.
/// </summary>
public sealed record ScraperHealthChangedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    string ScraperName,
    ScraperHealthStatus OldStatus,
    ScraperHealthStatus NewStatus,
    decimal? SuccessRate,
    string? ErrorMessage,
    DateTime? LastSuccessfulScrape
) : IIntegrationEvent;

// src/Modules/Acquisition/AutomatedMarketIntelligenceTool.Acquisition.Contracts/Commands/StartScrapeCommand.cs
namespace AutomatedMarketIntelligenceTool.Acquisition.Contracts.Commands;

/// <summary>
/// Command to initiate a scraping operation.
/// </summary>
public sealed record StartScrapeCommand(
    Guid TenantId,
    string ScraperName,
    Dictionary<string, object> SearchParameters,
    int? MaxResults
);
```

### 2.3 Deduplication Module Contracts

```csharp
// src/Modules/Deduplication/AutomatedMarketIntelligenceTool.Deduplication.Contracts/Events/DuplicateCheckCompletedEvent.cs
namespace AutomatedMarketIntelligenceTool.Deduplication.Contracts.Events;

public enum MatchType
{
    None,
    ExactVin,
    ExactExternalId,
    FuzzyMatch,
    ImageMatch,
    Combined
}

/// <summary>
/// Published when duplicate checking completes for a listing.
/// </summary>
public sealed record DuplicateCheckCompletedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ListingId,
    bool IsDuplicate,
    Guid? DuplicateOfListingId,
    MatchType MatchType,
    decimal ConfidenceScore,
    Dictionary<string, decimal>? MatchDetails
) : IIntegrationEvent;

// src/Modules/Deduplication/AutomatedMarketIntelligenceTool.Deduplication.Contracts/Events/ReviewItemCreatedEvent.cs
namespace AutomatedMarketIntelligenceTool.Deduplication.Contracts.Events;

/// <summary>
/// Published when a potential duplicate requires manual review.
/// </summary>
public sealed record ReviewItemCreatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ReviewItemId,
    Guid ListingId,
    Guid PotentialDuplicateListingId,
    MatchType MatchType,
    decimal ConfidenceScore,
    string? ReviewReason
) : IIntegrationEvent;
```

### 2.4 Alerting Module Contracts

```csharp
// src/Modules/Alerting/AutomatedMarketIntelligenceTool.Alerting.Contracts/Events/AlertTriggeredEvent.cs
namespace AutomatedMarketIntelligenceTool.Alerting.Contracts.Events;

public enum AlertTriggerReason
{
    NewListingMatch,
    PriceDropMatch,
    PriceChangeMatch,
    RelistingDetected
}

/// <summary>
/// Published when an alert is triggered by a matching listing.
/// </summary>
public sealed record AlertTriggeredEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid AlertId,
    string AlertName,
    Guid ListingId,
    AlertTriggerReason Reason,
    Dictionary<string, object> MatchedCriteria,
    Guid? UserId
) : IIntegrationEvent;

// src/Modules/Alerting/AutomatedMarketIntelligenceTool.Alerting.Contracts/Events/NotificationSentEvent.cs
namespace AutomatedMarketIntelligenceTool.Alerting.Contracts.Events;

public enum NotificationChannel
{
    Email,
    Sms,
    Push,
    InApp,
    Webhook
}

/// <summary>
/// Published when a notification is sent to a user.
/// </summary>
public sealed record NotificationSentEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid NotificationId,
    Guid AlertId,
    NotificationChannel Channel,
    bool Success,
    string? FailureReason
) : IIntegrationEvent;
```

---

## 3. Event Bus Implementation

### 3.1 RabbitMQ Implementation with MassTransit

```csharp
// src/Shared/AutomatedMarketIntelligenceTool.Shared.Infrastructure/EventBus/RabbitMQ/RabbitMQEventBus.cs
using MassTransit;

namespace AutomatedMarketIntelligenceTool.Shared.Infrastructure.EventBus.RabbitMQ;

public sealed class RabbitMQEventBus : IEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<RabbitMQEventBus> _logger;

    public RabbitMQEventBus(
        IPublishEndpoint publishEndpoint,
        ICorrelationContext correlationContext,
        ILogger<RabbitMQEventBus> logger)
    {
        _publishEndpoint = publishEndpoint;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        _logger.LogInformation(
            "Publishing event {EventType} with ID {EventId} for tenant {TenantId}",
            typeof(TEvent).Name,
            @event.EventId,
            @event.TenantId);

        await _publishEndpoint.Publish(@event, context =>
        {
            context.CorrelationId = Guid.Parse(_correlationContext.CorrelationId);
            context.Headers.Set("CausationId", _correlationContext.CausationId);
            context.Headers.Set("Version", @event.Version);
        }, cancellationToken);
    }

    public async Task PublishManyAsync<TEvent>(
        IEnumerable<TEvent> events,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        foreach (var @event in events)
        {
            await PublishAsync(@event, cancellationToken);
        }
    }
}

// src/Shared/AutomatedMarketIntelligenceTool.Shared.Infrastructure/EventBus/RabbitMQ/RabbitMQExtensions.cs
namespace AutomatedMarketIntelligenceTool.Shared.Infrastructure.EventBus.RabbitMQ;

public static class RabbitMQExtensions
{
    public static IServiceCollection AddRabbitMQEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        var options = configuration.GetSection("RabbitMQ").Get<RabbitMQOptions>()
            ?? throw new InvalidOperationException("RabbitMQ configuration is required");

        services.AddMassTransit(x =>
        {
            // Configure consumers from each module
            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, options.Port, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                // Configure retry policies
                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(5)));

                // Configure dead letter queue
                cfg.UseDelayedRedelivery(r => r.Intervals(
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(15)));

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventBus, RabbitMQEventBus>();

        return services;
    }
}
```

### 3.2 Transactional Outbox Pattern

```csharp
// src/Shared/AutomatedMarketIntelligenceTool.Shared.Infrastructure/EventBus/Outbox/OutboxMessage.cs
namespace AutomatedMarketIntelligenceTool.Shared.Infrastructure.EventBus.Outbox;

/// <summary>
/// Represents an event stored in the outbox for reliable delivery.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = null!;
    public string EventPayload { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }

    public static OutboxMessage Create<TEvent>(TEvent @event) where TEvent : IIntegrationEvent
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = typeof(TEvent).AssemblyQualifiedName!,
            EventPayload = JsonSerializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        RetryCount++;
        Error = error;
    }
}

// src/Shared/AutomatedMarketIntelligenceTool.Shared.Infrastructure/EventBus/Outbox/OutboxProcessor.cs
namespace AutomatedMarketIntelligenceTool.Shared.Infrastructure.EventBus.Outbox;

/// <summary>
/// Background service that processes outbox messages and publishes to the event bus.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxOptions _options;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_options.ProcessingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null && m.RetryCount < _options.MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.EventType)!;
                var @event = JsonSerializer.Deserialize(message.EventPayload, eventType)!;

                await ((dynamic)eventBus).PublishAsync((dynamic)@event, cancellationToken);

                message.MarkAsProcessed();
                _logger.LogDebug("Processed outbox message {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                message.MarkAsFailed(ex.Message);
                _logger.LogWarning(ex, "Failed to process outbox message {MessageId}", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

---

## 4. Event Handler Implementation Examples

### 4.1 Listings Module: Handling Scraped Listings

```csharp
// src/Modules/Listings/AutomatedMarketIntelligenceTool.Listings.Application/EventHandlers/
//   ScrapedListingAvailableHandler.cs
namespace AutomatedMarketIntelligenceTool.Listings.Application.EventHandlers;

public sealed class ScrapedListingAvailableHandler
    : IIntegrationEventHandler<ScrapedListingAvailableEvent>
{
    private readonly IListingsDbContext _dbContext;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ScrapedListingAvailableHandler> _logger;

    public ScrapedListingAvailableHandler(
        IListingsDbContext dbContext,
        IEventBus eventBus,
        ILogger<ScrapedListingAvailableHandler> logger)
    {
        _dbContext = dbContext;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(
        ScrapedListingAvailableEvent @event,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing scraped listing {ExternalId} from {Source}",
            @event.ExternalId,
            @event.SourceSite);

        // Check if listing already exists
        var existingListing = await _dbContext.Listings
            .FirstOrDefaultAsync(l =>
                l.TenantId == @event.TenantId &&
                l.SourceSite == @event.SourceSite &&
                l.ExternalId == @event.ExternalId,
                cancellationToken);

        if (existingListing is null)
        {
            // Create new listing
            var listing = Listing.Create(
                @event.TenantId,
                @event.Data.Make ?? "Unknown",
                @event.Data.Model ?? "Unknown",
                @event.Data.Year ?? 0,
                @event.Data.Price ?? 0,
                @event.SourceSite,
                @event.ExternalId);

            listing.UpdateDetails(
                @event.Data.Mileage,
                @event.Data.Location,
                @event.Data.PostalCode,
                @event.Data.Vin,
                @event.Data.Condition,
                @event.Data.SellerType);

            await _dbContext.Listings.AddAsync(listing, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Publish ListingCreated event
            await _eventBus.PublishAsync(new ListingCreatedEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTime.UtcNow,
                TenantId: @event.TenantId,
                ListingId: listing.Id,
                Make: listing.Make,
                Model: listing.Model,
                Year: listing.Year,
                Price: listing.Price,
                Mileage: listing.Mileage,
                Location: listing.Location,
                PostalCode: listing.PostalCode,
                SourceSite: listing.SourceSite,
                ExternalId: listing.ExternalId,
                Vin: listing.Vin,
                Condition: listing.Condition,
                SellerType: listing.SellerType
            ), cancellationToken);
        }
        else
        {
            // Update existing listing and check for price changes
            var oldPrice = existingListing.Price;
            existingListing.UpdateFromScrapedData(@event.Data);

            if (oldPrice != existingListing.Price)
            {
                await _eventBus.PublishAsync(new ListingPriceChangedEvent(
                    EventId: Guid.NewGuid(),
                    OccurredAt: DateTime.UtcNow,
                    TenantId: @event.TenantId,
                    ListingId: existingListing.Id,
                    Make: existingListing.Make,
                    Model: existingListing.Model,
                    Year: existingListing.Year,
                    OldPrice: oldPrice,
                    NewPrice: existingListing.Price,
                    ChangeAmount: existingListing.Price - oldPrice,
                    ChangePercentage: ((existingListing.Price - oldPrice) / oldPrice) * 100
                ), cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
```

### 4.2 Alerting Module: Handling New Listings

```csharp
// src/Modules/Alerting/AutomatedMarketIntelligenceTool.Alerting.Application/EventHandlers/
//   ListingCreatedHandler.cs
namespace AutomatedMarketIntelligenceTool.Alerting.Application.EventHandlers;

public sealed class ListingCreatedHandler : IIntegrationEventHandler<ListingCreatedEvent>
{
    private readonly IAlertingDbContext _dbContext;
    private readonly IAlertMatcher _alertMatcher;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ListingCreatedHandler> _logger;

    public ListingCreatedHandler(
        IAlertingDbContext dbContext,
        IAlertMatcher alertMatcher,
        IEventBus eventBus,
        ILogger<ListingCreatedHandler> logger)
    {
        _dbContext = dbContext;
        _alertMatcher = alertMatcher;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleAsync(
        ListingCreatedEvent @event,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Checking alerts for new listing {ListingId}",
            @event.ListingId);

        // Store a snapshot of the listing for local queries
        await StoreListingSnapshotAsync(@event, cancellationToken);

        // Get all active alerts for this tenant
        var activeAlerts = await _dbContext.Alerts
            .Where(a => a.TenantId == @event.TenantId && a.IsActive)
            .ToListAsync(cancellationToken);

        // Check each alert against the new listing
        foreach (var alert in activeAlerts)
        {
            var matchResult = _alertMatcher.CheckMatch(alert, @event);

            if (matchResult.IsMatch)
            {
                _logger.LogInformation(
                    "Alert {AlertId} triggered for listing {ListingId}",
                    alert.Id,
                    @event.ListingId);

                // Create notification record
                var notification = AlertNotification.Create(
                    alert.Id,
                    @event.ListingId,
                    AlertTriggerReason.NewListingMatch,
                    matchResult.MatchedCriteria);

                await _dbContext.AlertNotifications.AddAsync(notification, cancellationToken);

                // Publish event for notification delivery
                await _eventBus.PublishAsync(new AlertTriggeredEvent(
                    EventId: Guid.NewGuid(),
                    OccurredAt: DateTime.UtcNow,
                    TenantId: @event.TenantId,
                    AlertId: alert.Id,
                    AlertName: alert.Name,
                    ListingId: @event.ListingId,
                    Reason: AlertTriggerReason.NewListingMatch,
                    MatchedCriteria: matchResult.MatchedCriteria,
                    UserId: alert.UserId
                ), cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task StoreListingSnapshotAsync(
        ListingCreatedEvent @event,
        CancellationToken cancellationToken)
    {
        var snapshot = new ListingSnapshot
        {
            ListingId = @event.ListingId,
            TenantId = @event.TenantId,
            Make = @event.Make,
            Model = @event.Model,
            Year = @event.Year,
            Price = @event.Price,
            Mileage = @event.Mileage,
            Location = @event.Location,
            PostalCode = @event.PostalCode,
            LastUpdatedAt = @event.OccurredAt
        };

        await _dbContext.ListingSnapshots.AddAsync(snapshot, cancellationToken);
    }
}
```

### 4.3 Analytics Module: Recording Events

```csharp
// src/Modules/Analytics/AutomatedMarketIntelligenceTool.Analytics.Application/EventHandlers/
//   ListingEventsHandler.cs
namespace AutomatedMarketIntelligenceTool.Analytics.Application.EventHandlers;

public sealed class ListingEventsHandler :
    IIntegrationEventHandler<ListingCreatedEvent>,
    IIntegrationEventHandler<ListingPriceChangedEvent>,
    IIntegrationEventHandler<ListingDeactivatedEvent>
{
    private readonly IAnalyticsDbContext _dbContext;
    private readonly ILogger<ListingEventsHandler> _logger;

    public ListingEventsHandler(
        IAnalyticsDbContext dbContext,
        ILogger<ListingEventsHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(
        ListingCreatedEvent @event,
        CancellationToken cancellationToken)
    {
        var record = new ListingAnalyticsRecord
        {
            EventType = "ListingCreated",
            TenantId = @event.TenantId,
            ListingId = @event.ListingId,
            Make = @event.Make,
            Model = @event.Model,
            Year = @event.Year,
            Price = @event.Price,
            Mileage = @event.Mileage,
            Location = @event.Location,
            SourceSite = @event.SourceSite,
            OccurredAt = @event.OccurredAt
        };

        await _dbContext.ListingAnalytics.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(
        ListingPriceChangedEvent @event,
        CancellationToken cancellationToken)
    {
        var record = new PriceChangeAnalyticsRecord
        {
            TenantId = @event.TenantId,
            ListingId = @event.ListingId,
            Make = @event.Make,
            Model = @event.Model,
            Year = @event.Year,
            OldPrice = @event.OldPrice,
            NewPrice = @event.NewPrice,
            ChangePercentage = @event.ChangePercentage,
            OccurredAt = @event.OccurredAt
        };

        await _dbContext.PriceChangeAnalytics.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(
        ListingDeactivatedEvent @event,
        CancellationToken cancellationToken)
    {
        var record = new ListingAnalyticsRecord
        {
            EventType = "ListingDeactivated",
            TenantId = @event.TenantId,
            ListingId = @event.ListingId,
            DeactivationReason = @event.Reason.ToString(),
            OccurredAt = @event.OccurredAt
        };

        await _dbContext.ListingAnalytics.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

---

## 5. API Client Implementation

### 5.1 Typed HTTP Clients

```csharp
// src/Modules/Listings/AutomatedMarketIntelligenceTool.Listings.Contracts/IListingsApiClient.cs
namespace AutomatedMarketIntelligenceTool.Listings.Contracts;

/// <summary>
/// HTTP client interface for the Listings module API.
/// </summary>
public interface IListingsApiClient
{
    Task<ListingDto?> GetListingAsync(Guid listingId, CancellationToken cancellationToken = default);
    Task<PagedResult<ListingDto>> SearchListingsAsync(SearchListingsRequest request, CancellationToken cancellationToken = default);
}

// src/Modules/Listings/AutomatedMarketIntelligenceTool.Listings.Contracts/ListingsApiClient.cs
namespace AutomatedMarketIntelligenceTool.Listings.Contracts;

public sealed class ListingsApiClient : IListingsApiClient
{
    private readonly HttpClient _httpClient;

    public ListingsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ListingDto?> GetListingAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/listings/{listingId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ListingDto>(cancellationToken);
    }

    public async Task<PagedResult<ListingDto>> SearchListingsAsync(
        SearchListingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/listings/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PagedResult<ListingDto>>(cancellationToken)
            ?? new PagedResult<ListingDto>();
    }
}

// Registration with resilience policies
public static class ListingsApiClientExtensions
{
    public static IServiceCollection AddListingsApiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient<IListingsApiClient, ListingsApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:Listings:BaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
```

---

## 6. Testing Event-Driven Systems

### 6.1 Integration Test Infrastructure

```csharp
// tests/AutomatedMarketIntelligenceTool.IntegrationTests/Infrastructure/TestEventBus.cs
namespace AutomatedMarketIntelligenceTool.IntegrationTests.Infrastructure;

/// <summary>
/// In-memory event bus for integration testing.
/// </summary>
public sealed class TestEventBus : IEventBus
{
    private readonly List<IIntegrationEvent> _publishedEvents = new();
    private readonly Dictionary<Type, List<object>> _handlers = new();

    public IReadOnlyList<IIntegrationEvent> PublishedEvents => _publishedEvents.AsReadOnly();

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        _publishedEvents.Add(@event);

        // Invoke registered handlers
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            foreach (var handler in handlers.Cast<IIntegrationEventHandler<TEvent>>())
            {
                handler.HandleAsync(@event, cancellationToken);
            }
        }

        return Task.CompletedTask;
    }

    public Task PublishManyAsync<TEvent>(
        IEnumerable<TEvent> events,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        foreach (var @event in events)
        {
            PublishAsync(@event, cancellationToken);
        }
        return Task.CompletedTask;
    }

    public void RegisterHandler<TEvent>(IIntegrationEventHandler<TEvent> handler)
        where TEvent : IIntegrationEvent
    {
        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<object>();
        }
        _handlers[eventType].Add(handler);
    }

    public void Clear() => _publishedEvents.Clear();

    public TEvent? GetPublishedEvent<TEvent>() where TEvent : IIntegrationEvent
    {
        return _publishedEvents.OfType<TEvent>().LastOrDefault();
    }

    public IEnumerable<TEvent> GetPublishedEvents<TEvent>() where TEvent : IIntegrationEvent
    {
        return _publishedEvents.OfType<TEvent>();
    }
}

// tests/AutomatedMarketIntelligenceTool.IntegrationTests/Listings/ListingCreationTests.cs
namespace AutomatedMarketIntelligenceTool.IntegrationTests.Listings;

public class ListingCreationTests : IClassFixture<ListingsModuleFixture>
{
    private readonly ListingsModuleFixture _fixture;

    public ListingCreationTests(ListingsModuleFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task When_ScrapedListingReceived_Should_CreateListing_And_PublishEvent()
    {
        // Arrange
        var scrapedEvent = new ScrapedListingAvailableEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: _fixture.TenantId,
            ScrapedListingId: Guid.NewGuid(),
            SourceSite: "autotrader",
            ExternalId: "12345",
            Url: "https://autotrader.ca/listing/12345",
            Data: new ScrapedListingData(
                Title: "2020 Honda Civic",
                Make: "Honda",
                Model: "Civic",
                Year: 2020,
                Price: 25000,
                Mileage: 50000,
                Location: "Toronto",
                PostalCode: "M5V 1A1",
                Vin: "1HGBH41JXMN109186",
                Condition: "Used",
                Description: "Well maintained",
                SellerName: "AutoDealer",
                SellerType: "Dealer",
                ImageUrls: new List<string>(),
                AdditionalAttributes: new Dictionary<string, string>(),
                ScrapedAt: DateTime.UtcNow));

        // Act
        await _fixture.Handler.HandleAsync(scrapedEvent, CancellationToken.None);

        // Assert
        var listing = await _fixture.DbContext.Listings
            .FirstOrDefaultAsync(l => l.ExternalId == "12345");

        listing.Should().NotBeNull();
        listing!.Make.Should().Be("Honda");
        listing.Model.Should().Be("Civic");
        listing.Year.Should().Be(2020);
        listing.Price.Should().Be(25000);

        var publishedEvent = _fixture.EventBus.GetPublishedEvent<ListingCreatedEvent>();
        publishedEvent.Should().NotBeNull();
        publishedEvent!.ListingId.Should().Be(listing.Id);
        publishedEvent.Make.Should().Be("Honda");
    }
}
```

---

## 7. Module Registration & Startup

### 7.1 Module Bootstrapper

```csharp
// src/Modules/Listings/AutomatedMarketIntelligenceTool.Listings.Api/ListingsModule.cs
namespace AutomatedMarketIntelligenceTool.Listings.Api;

public sealed class ListingsModule : IModule
{
    public string Name => "Listings";
    public string Version => "1.0.0";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Domain services
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<IPriceHistoryService, PriceHistoryService>();
        services.AddScoped<IDealerService, DealerService>();

        // Application services
        services.AddScoped<IIntegrationEventHandler<ScrapedListingAvailableEvent>,
            ScrapedListingAvailableHandler>();

        // Infrastructure
        services.AddDbContext<ListingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ListingsDb")));

        services.AddScoped<IListingsDbContext>(sp =>
            sp.GetRequiredService<ListingsDbContext>());
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/listings")
            .WithTags("Listings");

        group.MapGet("/{id:guid}", GetListing);
        group.MapPost("/", CreateListing);
        group.MapPost("/search", SearchListings);
        group.MapPut("/{id:guid}", UpdateListing);
        group.MapDelete("/{id:guid}", DeactivateListing);
    }

    public void ConfigureEventHandlers(IEventBusBuilder builder)
    {
        builder.Subscribe<ScrapedListingAvailableEvent, ScrapedListingAvailableHandler>();
    }

    public async Task InitializeAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ListingsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    // Endpoint implementations...
    private static async Task<IResult> GetListing(
        Guid id,
        IListingService listingService,
        CancellationToken cancellationToken)
    {
        var listing = await listingService.GetByIdAsync(id, cancellationToken);
        return listing is null ? Results.NotFound() : Results.Ok(listing);
    }

    // ... other endpoints
}
```

### 7.2 Modular Host Configuration

```csharp
// src/Gateway/AutomatedMarketIntelligenceTool.Gateway/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add shared infrastructure
builder.Services.AddRabbitMQEventBus(builder.Configuration, cfg =>
{
    // Each module registers its consumers
    cfg.AddConsumer<ScrapedListingAvailableHandler>();
    cfg.AddConsumer<ListingCreatedHandler>();
    cfg.AddConsumer<ListingPriceChangedHandler>();
    // ... other handlers
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit")
        .AddJaegerExporter());

// Add API Gateway
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<RabbitMQHealthCheck>("rabbitmq")
    .AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

app.UseHealthChecks("/health");
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapReverseProxy();

app.Run();
```

---

## Summary

This implementation guide provides:

1. **Shared Abstractions** - Common interfaces for events, commands, queries, and contexts
2. **Module Contracts** - Event and API contract definitions for each bounded context
3. **Event Bus Implementation** - RabbitMQ with MassTransit and transactional outbox
4. **Event Handlers** - Examples showing cross-module communication patterns
5. **API Clients** - Typed HTTP clients with resilience policies
6. **Testing Infrastructure** - In-memory event bus for integration testing
7. **Module Registration** - Bootstrapping and startup configuration

Each module can be developed, tested, and deployed independently while maintaining loose coupling through well-defined contracts.
