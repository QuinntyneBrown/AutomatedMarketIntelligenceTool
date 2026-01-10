# Event-Driven Modular Architecture Design

## Executive Summary

This document outlines the architectural design for transforming the AutomatedMarketIntelligenceTool from a modular monolith into an event-driven modular system with independently deployable bounded contexts, each with its own dedicated data store.

### Goals

1. **Independent Deployability**: Each bounded context can be updated, tested, and deployed independently
2. **Technology Autonomy**: Each module can choose its optimal data store and technology stack
3. **Scalability**: Scale individual modules based on their specific load patterns
4. **Resilience**: Failure in one module doesn't cascade to others
5. **Team Autonomy**: Teams can work on different modules without tight coordination

---

## Table of Contents

1. [Bounded Contexts Overview](#1-bounded-contexts-overview)
2. [Event-Driven Architecture](#2-event-driven-architecture)
3. [Data Store Strategy](#3-data-store-strategy)
4. [Module Structure](#4-module-structure)
5. [Integration Events Catalog](#5-integration-events-catalog)
6. [API Gateway & Routing](#6-api-gateway--routing)
7. [Deployment Architecture](#7-deployment-architecture)
8. [Migration Strategy](#8-migration-strategy)
9. [Cross-Cutting Concerns](#9-cross-cutting-concerns)
10. [Technology Stack](#10-technology-stack)

---

## 1. Bounded Contexts Overview

Based on domain analysis, the system is organized into **8 primary bounded contexts** (consolidated from the 12 identified for optimal cohesion):

### 1.1 Bounded Context Map

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           AUTOMATED MARKET INTELLIGENCE                          │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌──────────────────┐    Events    ┌──────────────────┐    Events              │
│  │   ACQUISITION    │─────────────►│    LISTINGS      │─────────────►          │
│  │   (Scraping)     │              │   (Core Domain)  │              │          │
│  └──────────────────┘              └──────────────────┘              │          │
│          │                                  │                         │          │
│          │                                  ▼                         ▼          │
│          │                         ┌──────────────────┐    ┌──────────────────┐ │
│          │                         │  DEDUPLICATION   │    │    ALERTING      │ │
│          │                         │   & MATCHING     │    │  & MONITORING    │ │
│          │                         └──────────────────┘    └──────────────────┘ │
│          │                                  │                         │          │
│          │                                  ▼                         │          │
│          │                         ┌──────────────────┐              │          │
│          └────────────────────────►│    ANALYTICS     │◄─────────────┘          │
│                                    │   & REPORTING    │                         │
│                                    └──────────────────┘                         │
│                                             │                                    │
│  ┌──────────────────┐              ┌──────────────────┐                         │
│  │  CONFIGURATION   │◄────────────►│     SEARCH       │                         │
│  │   & SETTINGS     │              │   & DISCOVERY    │                         │
│  └──────────────────┘              └──────────────────┘                         │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Bounded Context Definitions

| Context | Responsibility | Key Aggregates | Upstream Dependencies | Downstream Consumers |
|---------|---------------|----------------|----------------------|---------------------|
| **Acquisition** | Web scraping, data extraction, rate limiting | ScrapedListing, ScraperHealth, ResourceThrottle | Configuration | Listings, Analytics |
| **Listings** | Core listing data, lifecycle management | Listing, Dealer, PriceHistory | Acquisition | Deduplication, Alerting, Search, Analytics |
| **Deduplication** | Duplicate detection, fuzzy matching, review queue | DeduplicationConfig, DeduplicationAudit, ReviewItem | Listings | Listings (feedback), Analytics |
| **Alerting** | User alerts, notifications, watch lists | Alert, AlertNotification, WatchedListing | Listings | Analytics |
| **Search** | Search execution, profiles, sessions | SearchProfile, SearchSession | Listings, Configuration | Analytics |
| **Analytics** | Reporting, dashboards, statistics | Report, ScheduledReport | All contexts | None (terminal) |
| **Configuration** | Markets, settings, tenant config | CustomMarket, TenantSettings | None | All contexts |
| **Identity** | Users, authentication, authorization | User, Tenant, Role | None | All contexts |

---

## 2. Event-Driven Architecture

### 2.1 Event Types

The system uses two types of events:

#### Domain Events (Internal)
- Scoped within a single bounded context
- Synchronous, in-process
- Used for aggregate coordination within the same module

#### Integration Events (External)
- Cross bounded context communication
- Asynchronous, message-based
- Published to event bus for other modules to consume

### 2.2 Event Bus Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              EVENT BUS (Message Broker)                          │
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                        Topic/Exchange Layer                              │   │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐   │   │
│  │  │ acquisition │ │  listings   │ │   dedup     │ │    alerting     │   │   │
│  │  │   .events   │ │   .events   │ │   .events   │ │     .events     │   │   │
│  │  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────────┘   │   │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐   │   │
│  │  │   search    │ │  analytics  │ │   config    │ │    identity     │   │   │
│  │  │   .events   │ │   .events   │ │   .events   │ │     .events     │   │   │
│  │  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                     Dead Letter Queue (DLQ)                              │   │
│  │                     Retry Policies & Circuit Breakers                    │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 2.3 Event Flow Patterns

#### Pattern 1: Event Notification
Simple notification that something happened. Consumers fetch additional data if needed.

```csharp
// Publisher (Listings Module)
public record ListingCreatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid ListingId,
    Guid TenantId
) : IIntegrationEvent;

// Consumer (Alerting Module) - fetches full listing via API if needed
```

#### Pattern 2: Event-Carried State Transfer
Event contains all necessary data. No callback needed.

```csharp
// Publisher (Listings Module)
public record ListingPriceChangedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid ListingId,
    Guid TenantId,
    decimal OldPrice,
    decimal NewPrice,
    string Make,
    string Model,
    int Year
) : IIntegrationEvent;
```

#### Pattern 3: Event Sourcing (Optional for Audit-Heavy Contexts)
Store all state changes as events. Rebuild state by replaying events.

```csharp
// Deduplication Audit Context
public record DeduplicationDecisionEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid ListingId,
    Guid MatchedListingId,
    MatchType MatchType,
    decimal ConfidenceScore,
    DecisionType Decision,
    string Reason
) : IIntegrationEvent;
```

### 2.4 Event Contracts

All integration events must:
1. Be immutable (record types)
2. Include `EventId`, `OccurredAt`, and `TenantId`
3. Be versioned (schema evolution support)
4. Be serializable to JSON

```csharp
namespace AutomatedMarketIntelligenceTool.Contracts.Events;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    Guid TenantId { get; }
    int Version => 1;
}

public interface IIntegrationEventHandler<TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
```

---

## 3. Data Store Strategy

### 3.1 Database Per Bounded Context

Each bounded context owns its data and chooses the optimal storage technology:

| Context | Primary Data Store | Rationale | Secondary Store |
|---------|-------------------|-----------|-----------------|
| **Acquisition** | PostgreSQL | Complex queries, JSON support for raw data | Redis (rate limiting, caching) |
| **Listings** | PostgreSQL | Relational data, full-text search, geospatial | Elasticsearch (search optimization) |
| **Deduplication** | PostgreSQL + Redis | Relational for audit, Redis for matching cache | - |
| **Alerting** | PostgreSQL | Relational alert criteria | Redis (real-time matching) |
| **Search** | Elasticsearch | Optimized for full-text and faceted search | PostgreSQL (profiles) |
| **Analytics** | ClickHouse / TimescaleDB | Time-series data, fast aggregations | PostgreSQL (report metadata) |
| **Configuration** | PostgreSQL | Relational settings, tenant config | Redis (config cache) |
| **Identity** | PostgreSQL | User/tenant data, OAuth tokens | Redis (session cache) |

### 3.2 Data Isolation Strategy

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              DATA ISOLATION                                      │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐          │
│  │   acquisition    │    │     listings     │    │   deduplication  │          │
│  │   ────────────   │    │   ────────────   │    │   ────────────   │          │
│  │   PostgreSQL     │    │   PostgreSQL     │    │   PostgreSQL     │          │
│  │   ami_acquisition│    │   ami_listings   │    │   ami_dedup      │          │
│  │   + Redis        │    │   + Elasticsearch│    │   + Redis        │          │
│  └──────────────────┘    └──────────────────┘    └──────────────────┘          │
│                                                                                  │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐          │
│  │    alerting      │    │     search       │    │    analytics     │          │
│  │   ────────────   │    │   ────────────   │    │   ────────────   │          │
│  │   PostgreSQL     │    │   Elasticsearch  │    │   TimescaleDB    │          │
│  │   ami_alerting   │    │   ami_search     │    │   ami_analytics  │          │
│  │   + Redis        │    │   + PostgreSQL   │    │   + PostgreSQL   │          │
│  └──────────────────┘    └──────────────────┘    └──────────────────┘          │
│                                                                                  │
│  ┌──────────────────┐    ┌──────────────────┐                                   │
│  │  configuration   │    │    identity      │                                   │
│  │   ────────────   │    │   ────────────   │                                   │
│  │   PostgreSQL     │    │   PostgreSQL     │                                   │
│  │   ami_config     │    │   ami_identity   │                                   │
│  │   + Redis        │    │   + Redis        │                                   │
│  └──────────────────┘    └──────────────────┘                                   │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 3.3 Data Synchronization Patterns

#### Saga Pattern for Distributed Transactions
When operations span multiple bounded contexts:

```csharp
// Example: Processing a new scraped listing
public class ProcessScrapedListingSaga
{
    // Step 1: Acquisition publishes ScrapedListingAvailable
    // Step 2: Listings creates/updates listing, publishes ListingCreated/Updated
    // Step 3: Deduplication checks for duplicates, publishes DuplicateCheckCompleted
    // Step 4: Alerting checks against active alerts, publishes AlertsTriggered
    // Step 5: Analytics records the event for reporting

    // Compensating actions if any step fails
}
```

#### Eventual Consistency
- Each module maintains its own view of shared data
- Updates propagate via events (milliseconds to seconds delay)
- UI should handle "stale read" scenarios gracefully

### 3.4 Shared Data Antipattern - Avoided

**Wrong**: Multiple modules reading/writing to the same table
**Right**: Each module owns its data, exposes it via API or events

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          DATA OWNERSHIP BOUNDARIES                               │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│   Listings Module                     Alerting Module                            │
│   ┌───────────────────┐              ┌───────────────────┐                      │
│   │ listings table    │   Events     │ listing_snapshots │                      │
│   │ (source of truth) │─────────────►│ (local copy for   │                      │
│   │                   │              │  alert matching)  │                      │
│   └───────────────────┘              └───────────────────┘                      │
│           │                                    │                                 │
│           │ API                                │ API                            │
│           ▼                                    ▼                                 │
│   ┌───────────────────┐              ┌───────────────────┐                      │
│   │ GET /listings/{id}│              │ GET /alerts       │                      │
│   │ POST /listings    │              │ POST /alerts      │                      │
│   └───────────────────┘              └───────────────────┘                      │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Module Structure

### 4.1 Standard Module Layout

Each bounded context follows a consistent structure:

```
src/
├── Modules/
│   ├── Acquisition/
│   │   ├── AutomatedMarketIntelligenceTool.Acquisition.Api/
│   │   │   ├── Controllers/
│   │   │   ├── Program.cs
│   │   │   └── appsettings.json
│   │   ├── AutomatedMarketIntelligenceTool.Acquisition.Application/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   ├── EventHandlers/
│   │   │   └── Services/
│   │   ├── AutomatedMarketIntelligenceTool.Acquisition.Domain/
│   │   │   ├── Aggregates/
│   │   │   ├── Events/
│   │   │   ├── ValueObjects/
│   │   │   └── Services/
│   │   ├── AutomatedMarketIntelligenceTool.Acquisition.Infrastructure/
│   │   │   ├── Persistence/
│   │   │   ├── Scrapers/
│   │   │   ├── EventBus/
│   │   │   └── ExternalServices/
│   │   └── AutomatedMarketIntelligenceTool.Acquisition.Contracts/
│   │       ├── Events/
│   │       ├── Commands/
│   │       └── Queries/
│   │
│   ├── Listings/
│   │   └── [Same structure]
│   ├── Deduplication/
│   │   └── [Same structure]
│   ├── Alerting/
│   │   └── [Same structure]
│   ├── Search/
│   │   └── [Same structure]
│   ├── Analytics/
│   │   └── [Same structure]
│   ├── Configuration/
│   │   └── [Same structure]
│   └── Identity/
│       └── [Same structure]
│
├── Shared/
│   ├── AutomatedMarketIntelligenceTool.Shared.Abstractions/
│   │   ├── Events/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── Domain/
│   ├── AutomatedMarketIntelligenceTool.Shared.Infrastructure/
│   │   ├── EventBus/
│   │   ├── Messaging/
│   │   ├── Logging/
│   │   └── Observability/
│   └── AutomatedMarketIntelligenceTool.Shared.Contracts/
│       └── Common/
│
├── Gateway/
│   └── AutomatedMarketIntelligenceTool.Gateway/
│       ├── Program.cs
│       ├── ocelot.json
│       └── appsettings.json
│
└── Clients/
    ├── AutomatedMarketIntelligenceTool.Cli/
    └── AutomatedMarketIntelligenceTool.WebApp/
```

### 4.2 Module Independence Rules

1. **No Direct Module References**: Modules communicate only via:
   - Integration events (async)
   - HTTP/gRPC APIs (sync)
   - Shared contracts package

2. **Contracts Package**: Each module publishes a `*.Contracts` package containing:
   - Integration event definitions
   - API DTOs
   - Client interfaces

3. **Dependency Direction**:
   ```
   Domain ← Application ← Infrastructure ← API
                ↑
           Contracts (shared)
   ```

### 4.3 Module Interface Definition

```csharp
// Each module exposes a module definition
namespace AutomatedMarketIntelligenceTool.Acquisition.Api;

public class AcquisitionModule : IModule
{
    public string Name => "Acquisition";
    public string Version => "1.0.0";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register module services
        services.AddAcquisitionDomain();
        services.AddAcquisitionApplication();
        services.AddAcquisitionInfrastructure(configuration);
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Map module endpoints
        endpoints.MapAcquisitionEndpoints();
    }

    public void ConfigureEventHandlers(IEventBusBuilder builder)
    {
        // Subscribe to integration events
        builder.Subscribe<ListingCreatedEvent, ListingCreatedHandler>();
    }
}
```

---

## 5. Integration Events Catalog

### 5.1 Acquisition Module Events

```csharp
namespace AutomatedMarketIntelligenceTool.Acquisition.Contracts.Events;

// Published when raw listing data is scraped
public record ScrapedListingAvailableEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ScrapedListingId,
    string SourceSite,
    string ExternalId,
    string RawDataJson
) : IIntegrationEvent;

// Published when scraping health changes
public record ScraperHealthChangedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    string ScraperName,
    HealthStatus OldStatus,
    HealthStatus NewStatus,
    string? ErrorMessage
) : IIntegrationEvent;

// Published when rate limit is hit
public record RateLimitExceededEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    string ResourceKey,
    DateTime ResetAt
) : IIntegrationEvent;
```

### 5.2 Listings Module Events

```csharp
namespace AutomatedMarketIntelligenceTool.Listings.Contracts.Events;

// Published when a new listing is created
public record ListingCreatedEvent(
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
    string SourceSite,
    string ExternalId
) : IIntegrationEvent;

// Published when listing price changes
public record ListingPriceChangedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ListingId,
    decimal OldPrice,
    decimal NewPrice,
    decimal ChangePercentage
) : IIntegrationEvent;

// Published when listing is deactivated
public record ListingDeactivatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ListingId,
    DeactivationReason Reason
) : IIntegrationEvent;

// Published when listing is relisted
public record ListingRelistedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ListingId,
    Guid? PreviousListingId
) : IIntegrationEvent;

// Published when dealer info is updated
public record DealerUpdatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid DealerId,
    string DealerName,
    int TotalListings,
    decimal AverageRating
) : IIntegrationEvent;
```

### 5.3 Deduplication Module Events

```csharp
namespace AutomatedMarketIntelligenceTool.Deduplication.Contracts.Events;

// Published when duplicate check completes
public record DuplicateCheckCompletedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ListingId,
    bool IsDuplicate,
    Guid? DuplicateOfListingId,
    MatchType MatchType,
    decimal ConfidenceScore
) : IIntegrationEvent;

// Published when item is added to review queue
public record ReviewItemCreatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ReviewItemId,
    Guid ListingId,
    Guid? PotentialDuplicateListingId,
    decimal ConfidenceScore
) : IIntegrationEvent;

// Published when review decision is made
public record ReviewDecisionMadeEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ReviewItemId,
    ReviewDecision Decision,
    Guid? DecidedByUserId
) : IIntegrationEvent;
```

### 5.4 Alerting Module Events

```csharp
namespace AutomatedMarketIntelligenceTool.Alerting.Contracts.Events;

// Published when alert is triggered
public record AlertTriggeredEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid AlertId,
    Guid ListingId,
    AlertTriggerReason Reason,
    Dictionary<string, object> MatchedCriteria
) : IIntegrationEvent;

// Published when notification is sent
public record NotificationSentEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid NotificationId,
    Guid AlertId,
    NotificationChannel Channel,
    bool Success
) : IIntegrationEvent;

// Published when watch list item changes
public record WatchListItemStatusChangedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid WatchListItemId,
    Guid ListingId,
    WatchListChangeType ChangeType,
    Dictionary<string, object>? ChangeDetails
) : IIntegrationEvent;
```

### 5.5 Search Module Events

```csharp
namespace AutomatedMarketIntelligenceTool.Search.Contracts.Events;

// Published when search is executed
public record SearchExecutedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid SearchSessionId,
    Guid? SearchProfileId,
    Dictionary<string, object> SearchCriteria,
    int ResultCount,
    TimeSpan ExecutionTime
) : IIntegrationEvent;

// Published when search profile is created/updated
public record SearchProfileUpdatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid SearchProfileId,
    string ProfileName,
    Dictionary<string, object> Criteria
) : IIntegrationEvent;
```

### 5.6 Analytics Module Events

```csharp
namespace AutomatedMarketIntelligenceTool.Analytics.Contracts.Events;

// Published when report is generated
public record ReportGeneratedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ReportId,
    ReportType Type,
    ReportFormat Format,
    string FilePath
) : IIntegrationEvent;

// Published when scheduled report runs
public record ScheduledReportExecutedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ScheduledReportId,
    bool Success,
    string? ErrorMessage
) : IIntegrationEvent;
```

### 5.7 Configuration Module Events

```csharp
namespace AutomatedMarketIntelligenceTool.Configuration.Contracts.Events;

// Published when configuration changes
public record ConfigurationChangedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    string ConfigurationKey,
    string? OldValue,
    string? NewValue
) : IIntegrationEvent;

// Published when custom market is defined
public record CustomMarketDefinedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid CustomMarketId,
    string MarketName,
    List<string> PostalCodes
) : IIntegrationEvent;
```

### 5.8 Identity Module Events

```csharp
namespace AutomatedMarketIntelligenceTool.Identity.Contracts.Events;

// Published when tenant is created
public record TenantCreatedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    string TenantName,
    string Plan
) : IIntegrationEvent;

// Published when user joins tenant
public record UserJoinedTenantEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid UserId,
    string Email,
    string Role
) : IIntegrationEvent;
```

---

## 6. API Gateway & Routing

### 6.1 Gateway Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                               API GATEWAY                                        │
│                          (YARP / Ocelot / Kong)                                 │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                        Cross-Cutting Middleware                          │   │
│  │  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐ │   │
│  │  │   Auth    │ │   Rate    │ │  Logging  │ │  Caching  │ │  Circuit  │ │   │
│  │  │   JWT     │ │  Limiting │ │  Tracing  │ │  Redis    │ │  Breaker  │ │   │
│  │  └───────────┘ └───────────┘ └───────────┘ └───────────┘ └───────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                           Route Configuration                            │   │
│  │                                                                          │   │
│  │  /api/v1/acquisition/*  ──────────► Acquisition Service (port 5001)     │   │
│  │  /api/v1/listings/*     ──────────► Listings Service (port 5002)        │   │
│  │  /api/v1/dedup/*        ──────────► Deduplication Service (port 5003)   │   │
│  │  /api/v1/alerts/*       ──────────► Alerting Service (port 5004)        │   │
│  │  /api/v1/search/*       ──────────► Search Service (port 5005)          │   │
│  │  /api/v1/analytics/*    ──────────► Analytics Service (port 5006)       │   │
│  │  /api/v1/config/*       ──────────► Configuration Service (port 5007)   │   │
│  │  /api/v1/identity/*     ──────────► Identity Service (port 5008)        │   │
│  │                                                                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 6.2 YARP Configuration

```csharp
// Program.cs for API Gateway
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapReverseProxy();

app.Run();
```

```json
// appsettings.json for Gateway
{
  "ReverseProxy": {
    "Routes": {
      "acquisition-route": {
        "ClusterId": "acquisition",
        "Match": { "Path": "/api/v1/acquisition/{**catch-all}" },
        "Transforms": [{ "PathRemovePrefix": "/api/v1/acquisition" }]
      },
      "listings-route": {
        "ClusterId": "listings",
        "Match": { "Path": "/api/v1/listings/{**catch-all}" },
        "Transforms": [{ "PathRemovePrefix": "/api/v1/listings" }]
      }
    },
    "Clusters": {
      "acquisition": {
        "Destinations": {
          "acquisition-1": { "Address": "http://acquisition:5001" }
        }
      },
      "listings": {
        "Destinations": {
          "listings-1": { "Address": "http://listings:5002" }
        }
      }
    }
  }
}
```

### 6.3 Backend-for-Frontend (BFF)

For the CLI and WebApp, create specialized BFFs:

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                                                                                  │
│    ┌─────────────┐                ┌─────────────────────┐                       │
│    │    CLI      │───────────────►│    CLI BFF          │                       │
│    │   Client    │                │  (Optimized for     │                       │
│    └─────────────┘                │   CLI workflows)    │                       │
│                                   └─────────────────────┘                       │
│                                            │                                     │
│                                            ▼                                     │
│    ┌─────────────┐                ┌─────────────────────┐                       │
│    │   Web App   │───────────────►│    Web BFF          │───────► API Gateway  │
│    │   (Angular) │                │  (Optimized for     │                       │
│    └─────────────┘                │   web workflows)    │                       │
│                                   └─────────────────────┘                       │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Deployment Architecture

### 7.1 Container Architecture

```yaml
# docker-compose.yml
version: '3.8'

services:
  # API Gateway
  gateway:
    build: ./src/Gateway
    ports:
      - "5000:5000"
    depends_on:
      - acquisition
      - listings
      - deduplication
      - alerting
      - search
      - analytics
      - configuration
      - identity
    networks:
      - ami-network

  # Bounded Context Services
  acquisition:
    build: ./src/Modules/Acquisition/AutomatedMarketIntelligenceTool.Acquisition.Api
    environment:
      - ConnectionStrings__AcquisitionDb=Host=acquisition-db;Database=ami_acquisition
      - EventBus__Host=rabbitmq
    depends_on:
      - acquisition-db
      - rabbitmq
      - redis
    networks:
      - ami-network

  listings:
    build: ./src/Modules/Listings/AutomatedMarketIntelligenceTool.Listings.Api
    environment:
      - ConnectionStrings__ListingsDb=Host=listings-db;Database=ami_listings
      - EventBus__Host=rabbitmq
    depends_on:
      - listings-db
      - elasticsearch
      - rabbitmq
    networks:
      - ami-network

  deduplication:
    build: ./src/Modules/Deduplication/AutomatedMarketIntelligenceTool.Deduplication.Api
    environment:
      - ConnectionStrings__DedupDb=Host=dedup-db;Database=ami_dedup
      - EventBus__Host=rabbitmq
    depends_on:
      - dedup-db
      - rabbitmq
      - redis
    networks:
      - ami-network

  alerting:
    build: ./src/Modules/Alerting/AutomatedMarketIntelligenceTool.Alerting.Api
    environment:
      - ConnectionStrings__AlertingDb=Host=alerting-db;Database=ami_alerting
      - EventBus__Host=rabbitmq
    depends_on:
      - alerting-db
      - rabbitmq
      - redis
    networks:
      - ami-network

  search:
    build: ./src/Modules/Search/AutomatedMarketIntelligenceTool.Search.Api
    environment:
      - ConnectionStrings__SearchDb=Host=search-db;Database=ami_search
      - Elasticsearch__Url=http://elasticsearch:9200
      - EventBus__Host=rabbitmq
    depends_on:
      - search-db
      - elasticsearch
      - rabbitmq
    networks:
      - ami-network

  analytics:
    build: ./src/Modules/Analytics/AutomatedMarketIntelligenceTool.Analytics.Api
    environment:
      - ConnectionStrings__AnalyticsDb=Host=analytics-db;Database=ami_analytics
      - EventBus__Host=rabbitmq
    depends_on:
      - analytics-db
      - rabbitmq
    networks:
      - ami-network

  configuration:
    build: ./src/Modules/Configuration/AutomatedMarketIntelligenceTool.Configuration.Api
    environment:
      - ConnectionStrings__ConfigDb=Host=config-db;Database=ami_config
      - EventBus__Host=rabbitmq
    depends_on:
      - config-db
      - rabbitmq
      - redis
    networks:
      - ami-network

  identity:
    build: ./src/Modules/Identity/AutomatedMarketIntelligenceTool.Identity.Api
    environment:
      - ConnectionStrings__IdentityDb=Host=identity-db;Database=ami_identity
      - EventBus__Host=rabbitmq
    depends_on:
      - identity-db
      - rabbitmq
      - redis
    networks:
      - ami-network

  # Databases (one per bounded context)
  acquisition-db:
    image: postgres:16
    environment:
      POSTGRES_DB: ami_acquisition
      POSTGRES_USER: ami
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - acquisition-data:/var/lib/postgresql/data
    networks:
      - ami-network

  listings-db:
    image: postgres:16
    environment:
      POSTGRES_DB: ami_listings
      POSTGRES_USER: ami
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - listings-data:/var/lib/postgresql/data
    networks:
      - ami-network

  dedup-db:
    image: postgres:16
    environment:
      POSTGRES_DB: ami_dedup
      POSTGRES_USER: ami
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - dedup-data:/var/lib/postgresql/data
    networks:
      - ami-network

  alerting-db:
    image: postgres:16
    environment:
      POSTGRES_DB: ami_alerting
      POSTGRES_USER: ami
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - alerting-data:/var/lib/postgresql/data
    networks:
      - ami-network

  search-db:
    image: postgres:16
    environment:
      POSTGRES_DB: ami_search
      POSTGRES_USER: ami
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - search-data:/var/lib/postgresql/data
    networks:
      - ami-network

  analytics-db:
    image: timescale/timescaledb:latest-pg16
    environment:
      POSTGRES_DB: ami_analytics
      POSTGRES_USER: ami
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - analytics-data:/var/lib/postgresql/data
    networks:
      - ami-network

  config-db:
    image: postgres:16
    environment:
      POSTGRES_DB: ami_config
      POSTGRES_USER: ami
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - config-data:/var/lib/postgresql/data
    networks:
      - ami-network

  identity-db:
    image: postgres:16
    environment:
      POSTGRES_DB: ami_identity
      POSTGRES_USER: ami
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - identity-data:/var/lib/postgresql/data
    networks:
      - ami-network

  # Shared Infrastructure
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: ami
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD}
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq
    networks:
      - ami-network

  redis:
    image: redis:7-alpine
    volumes:
      - redis-data:/data
    networks:
      - ami-network

  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
    volumes:
      - elasticsearch-data:/usr/share/elasticsearch/data
    networks:
      - ami-network

networks:
  ami-network:
    driver: bridge

volumes:
  acquisition-data:
  listings-data:
  dedup-data:
  alerting-data:
  search-data:
  analytics-data:
  config-data:
  identity-data:
  rabbitmq-data:
  redis-data:
  elasticsearch-data:
```

### 7.2 Kubernetes Deployment (Production)

```yaml
# Example: listings-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: listings-service
  namespace: ami
spec:
  replicas: 3
  selector:
    matchLabels:
      app: listings
  template:
    metadata:
      labels:
        app: listings
    spec:
      containers:
      - name: listings
        image: ami/listings:latest
        ports:
        - containerPort: 5002
        env:
        - name: ConnectionStrings__ListingsDb
          valueFrom:
            secretKeyRef:
              name: listings-secrets
              key: connection-string
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5002
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /ready
            port: 5002
          initialDelaySeconds: 5
          periodSeconds: 10
---
apiVersion: v1
kind: Service
metadata:
  name: listings-service
  namespace: ami
spec:
  selector:
    app: listings
  ports:
  - port: 5002
    targetPort: 5002
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: listings-hpa
  namespace: ami
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: listings-service
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
```

---

## 8. Migration Strategy

### 8.1 Strangler Fig Pattern

Migrate incrementally from monolith to modular architecture:

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        MIGRATION PHASES                                          │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  PHASE 1: FOUNDATION                                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │ • Set up event bus infrastructure (RabbitMQ)                            │   │
│  │ • Create shared abstractions package                                     │   │
│  │ • Add integration event publishing to existing monolith                  │   │
│  │ • Deploy API Gateway in front of monolith                               │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  PHASE 2: IDENTITY & CONFIGURATION (Low Risk)                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │ • Extract Identity module first (authentication/authorization)           │   │
│  │ • Extract Configuration module                                           │   │
│  │ • Route /api/v1/identity/* to new service                               │   │
│  │ • Route /api/v1/config/* to new service                                 │   │
│  │ • Monolith becomes consumer of Identity events                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  PHASE 3: ACQUISITION (Medium Risk)                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │ • Extract scraping infrastructure to Acquisition module                  │   │
│  │ • Acquisition publishes ScrapedListingAvailable events                   │   │
│  │ • Monolith subscribes to create/update listings                         │   │
│  │ • Parallel run: both old and new scrapers for validation                │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  PHASE 4: LISTINGS (Core Domain)                                               │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │ • Extract Listings module with its own database                          │   │
│  │ • Migrate data from monolith to Listings database                        │   │
│  │ • Listings publishes domain events                                       │   │
│  │ • Other monolith components become event consumers                       │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  PHASE 5: DEDUPLICATION & ALERTING                                             │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │ • Extract Deduplication module                                           │   │
│  │ • Extract Alerting module                                                │   │
│  │ • Both subscribe to Listings events                                      │   │
│  │ • Each has its own database                                             │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  PHASE 6: SEARCH & ANALYTICS                                                   │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │ • Extract Search module with Elasticsearch                               │   │
│  │ • Extract Analytics module with TimescaleDB                              │   │
│  │ • Migrate historical data for analytics                                  │   │
│  │ • Retire monolith                                                        │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 8.2 Data Migration Strategy

```csharp
// Example: Migrate Listings data
public class ListingsMigrationService
{
    private readonly MonolithDbContext _monolithDb;
    private readonly ListingsDbContext _listingsDb;
    private readonly IEventBus _eventBus;

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        // 1. Initial bulk migration
        var listings = await _monolithDb.Listings
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var batch in listings.Chunk(1000))
        {
            var newListings = batch.Select(MapToNewListing).ToList();
            await _listingsDb.Listings.AddRangeAsync(newListings, cancellationToken);
            await _listingsDb.SaveChangesAsync(cancellationToken);
        }

        // 2. Set up CDC (Change Data Capture) for ongoing sync
        // Use Debezium or similar for real-time sync during transition

        // 3. Publish migration complete event
        await _eventBus.PublishAsync(new MigrationCompletedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: Guid.Empty, // System event
            Module: "Listings",
            RecordsMigrated: listings.Count
        ));
    }
}
```

### 8.3 Feature Flags for Gradual Rollout

```csharp
// Use feature flags to control traffic routing
public class ListingsRouter
{
    private readonly IFeatureManager _featureManager;
    private readonly IListingsApiClient _newListingsApi;
    private readonly IMonolithListingsService _monolithService;

    public async Task<Listing> GetListingAsync(Guid listingId)
    {
        if (await _featureManager.IsEnabledAsync("UseNewListingsModule"))
        {
            return await _newListingsApi.GetListingAsync(listingId);
        }

        return await _monolithService.GetListingAsync(listingId);
    }
}
```

---

## 9. Cross-Cutting Concerns

### 9.1 Observability Stack

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            OBSERVABILITY                                         │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                         Distributed Tracing                              │   │
│  │                                                                          │   │
│  │  ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐          │   │
│  │  │ OpenTelemetry │ ────────►│  Jaeger   │◄───────│ Zipkin   │          │   │
│  │  │  SDK      │    │  or     │    │ (alt)   │          │   │
│  │  └──────────┘    └──────────┘    └──────────┘    └──────────┘          │   │
│  │                                                                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                            Metrics                                       │   │
│  │                                                                          │   │
│  │  ┌──────────┐    ┌──────────┐    ┌──────────┐                           │   │
│  │  │Prometheus│───►│ Grafana  │    │ Alerts   │                           │   │
│  │  │          │    │          │───►│ Manager  │                           │   │
│  │  └──────────┘    └──────────┘    └──────────┘                           │   │
│  │                                                                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                            Logging                                       │   │
│  │                                                                          │   │
│  │  ┌──────────┐    ┌──────────┐    ┌──────────┐                           │   │
│  │  │ Serilog  │───►│   Seq    │    │ Elastic  │                           │   │
│  │  │          │    │   or     │───►│  Stack   │                           │   │
│  │  └──────────┘    └──────────┘    └──────────┘                           │   │
│  │                                                                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Correlation & Tracing

```csharp
// All modules use correlation IDs for request tracing
public interface ICorrelationContext
{
    string CorrelationId { get; }
    string CausationId { get; }
    Guid TenantId { get; }
    Guid? UserId { get; }
}

// Middleware to propagate correlation context
public class CorrelationMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"]
            .FirstOrDefault() ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        using var _ = LogContext.PushProperty("CorrelationId", correlationId);
        await next(context);
    }
}
```

### 9.3 Health Checks

```csharp
// Each module exposes health endpoints
public static class HealthCheckExtensions
{
    public static IServiceCollection AddModuleHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("Database")!,
                name: "database",
                tags: new[] { "db", "ready" })
            .AddRabbitMQ(
                configuration.GetConnectionString("RabbitMQ")!,
                name: "rabbitmq",
                tags: new[] { "messaging", "ready" })
            .AddRedis(
                configuration.GetConnectionString("Redis")!,
                name: "redis",
                tags: new[] { "cache", "ready" });

        return services;
    }
}
```

### 9.4 Circuit Breaker Pattern

```csharp
// Resilience for inter-module communication
services.AddHttpClient<IListingsApiClient, ListingsApiClient>()
    .AddPolicyHandler(Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => !r.IsSuccessStatusCode)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (result, duration) =>
            {
                Log.Warning("Circuit breaker opened for Listings API for {Duration}s",
                    duration.TotalSeconds);
            },
            onReset: () => Log.Information("Circuit breaker reset for Listings API")
        ))
    .AddPolicyHandler(Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

---

## 10. Technology Stack

### 10.1 Core Technologies

| Layer | Technology | Purpose |
|-------|------------|---------|
| **Runtime** | .NET 9 | Application framework |
| **API Gateway** | YARP | Reverse proxy, routing |
| **Event Bus** | RabbitMQ | Async messaging |
| **Primary DB** | PostgreSQL 16 | Relational data storage |
| **Time-Series DB** | TimescaleDB | Analytics data |
| **Search Engine** | Elasticsearch 8 | Full-text search |
| **Cache** | Redis 7 | Caching, rate limiting |
| **Container** | Docker | Containerization |
| **Orchestration** | Kubernetes | Production deployment |

### 10.2 Libraries & Frameworks

| Category | Library | Purpose |
|----------|---------|---------|
| **ORM** | EF Core 9 | Database access |
| **Messaging** | MassTransit | Message bus abstraction |
| **Resilience** | Polly | Retry, circuit breaker |
| **Logging** | Serilog | Structured logging |
| **Tracing** | OpenTelemetry | Distributed tracing |
| **API Docs** | Swagger/OpenAPI | API documentation |
| **Validation** | FluentValidation | Request validation |
| **Mapping** | Mapster | Object mapping |
| **Testing** | xUnit, NSubstitute | Unit/integration tests |

### 10.3 Development Tools

| Tool | Purpose |
|------|---------|
| **Aspire** | Local development orchestration |
| **Docker Compose** | Local multi-container development |
| **Seq** | Local log aggregation |
| **Jaeger** | Local distributed tracing |

---

## Appendix A: Event Schema Evolution

### Versioning Strategy

```csharp
// Support multiple versions of the same event
[EventVersion(1)]
public record ListingCreatedEventV1(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ListingId,
    string Make,
    string Model,
    int Year,
    decimal Price
) : IIntegrationEvent;

[EventVersion(2)]
public record ListingCreatedEventV2(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid ListingId,
    string Make,
    string Model,
    int Year,
    decimal Price,
    string? Vin,           // Added in V2
    string? ExternalId     // Added in V2
) : IIntegrationEvent;

// Event upgrader for backward compatibility
public class ListingCreatedEventUpgrader : IEventUpgrader<ListingCreatedEventV1, ListingCreatedEventV2>
{
    public ListingCreatedEventV2 Upgrade(ListingCreatedEventV1 v1) =>
        new(v1.EventId, v1.OccurredAt, v1.TenantId, v1.ListingId,
            v1.Make, v1.Model, v1.Year, v1.Price, null, null);
}
```

---

## Appendix B: Local Development Setup

### Using .NET Aspire

```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres");
var rabbitmq = builder.AddRabbitMQ("rabbitmq");
var redis = builder.AddRedis("redis");

// Databases per module
var acquisitionDb = postgres.AddDatabase("acquisition-db");
var listingsDb = postgres.AddDatabase("listings-db");
var dedupDb = postgres.AddDatabase("dedup-db");
var alertingDb = postgres.AddDatabase("alerting-db");
var searchDb = postgres.AddDatabase("search-db");
var analyticsDb = postgres.AddDatabase("analytics-db");
var configDb = postgres.AddDatabase("config-db");
var identityDb = postgres.AddDatabase("identity-db");

// Services
var acquisition = builder.AddProject<Projects.Acquisition_Api>("acquisition")
    .WithReference(acquisitionDb)
    .WithReference(rabbitmq)
    .WithReference(redis);

var listings = builder.AddProject<Projects.Listings_Api>("listings")
    .WithReference(listingsDb)
    .WithReference(rabbitmq);

// ... other services ...

// API Gateway
builder.AddProject<Projects.Gateway>("gateway")
    .WithReference(acquisition)
    .WithReference(listings);

builder.Build().Run();
```

---

## Appendix C: Decision Log

| Decision | Context | Options Considered | Decision | Rationale |
|----------|---------|-------------------|----------|-----------|
| Event Bus | Async communication | RabbitMQ, Kafka, Azure Service Bus | RabbitMQ | Simpler, sufficient for current scale, good .NET support |
| Primary DB | Module data storage | PostgreSQL, SQL Server, MySQL | PostgreSQL | Open source, JSON support, geospatial, full-text search |
| API Gateway | Request routing | YARP, Ocelot, Kong | YARP | Native .NET, high performance, simple config |
| Time-Series DB | Analytics | InfluxDB, TimescaleDB, ClickHouse | TimescaleDB | PostgreSQL compatible, easier operations |
| Message Bus Abstraction | Decouple from broker | MassTransit, NServiceBus, Raw RabbitMQ | MassTransit | Open source, good patterns, saga support |

---

## Summary

This design provides a path to transform the AutomatedMarketIntelligenceTool into a fully event-driven modular system where:

1. **8 Bounded Contexts** operate independently with their own databases
2. **Integration Events** enable loose coupling between modules
3. **API Gateway** provides unified access and cross-cutting concerns
4. **Strangler Fig Pattern** enables incremental migration
5. **Container-based Deployment** supports independent scaling and updates
6. **Observability Stack** provides visibility across all modules

The architecture maintains the existing Clean Architecture and DDD patterns while enabling team autonomy, independent deployability, and technology flexibility for each bounded context.
