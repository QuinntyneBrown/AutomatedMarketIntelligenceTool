# Coding Guidelines for C# Microservices and Angular Development

**Version:** 1.0  
**Last Updated:** 2026-01-16  
**Status:** Active

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [System-Wide Standards](#2-system-wide-standards)
3. [Backend Architecture](#3-backend-architecture)
4. [Core Project Guidelines](#4-core-project-guidelines)
5. [Infrastructure Project Guidelines](#5-infrastructure-project-guidelines)
6. [API Project Guidelines](#6-api-project-guidelines)
7. [Frontend Guidelines (Angular)](#7-frontend-guidelines-angular)
8. [Messaging and Event-Driven Architecture](#8-messaging-and-event-driven-architecture)
9. [Code Quality and Linting](#9-code-quality-and-linting)
10. [Testing Standards](#10-testing-standards)

---

## 1. Introduction

### 1.1 Purpose

This document establishes coding standards and architectural guidelines for developing C# microservices with Angular frontends. These guidelines ensure consistency, maintainability, and scalability across development teams.

### 1.2 Scope

This specification covers:

- Backend development using .NET and C#
- Frontend development using Angular
- Microservices architecture patterns
- Event-driven communication
- Code quality and testing standards

### 1.3 Definitions and Acronyms

| Term | Definition |
|------|------------|
| API | Application Programming Interface |
| BEM | Block Element Modifier (CSS naming methodology) |
| CORS | Cross-Origin Resource Sharing |
| CQRS | Command Query Responsibility Segregation |
| DDD | Domain-Driven Design |
| DTO | Data Transfer Object |
| EF | Entity Framework |

---

## 2. System-Wide Standards

### 2.1 Namespace Architecture

**CRITICAL**: All backend code namespaces SHALL exactly match the file's physical location within the project structure.

**Compliant Example:**

```csharp
// File: {AppName}.Core/Models/{Aggregate}Aggregate/Events/{Event}.cs
namespace {AppName}.Core.Models.{Aggregate}Aggregate.Events;
```

**Non-Compliant Example:**

```csharp
// File: {AppName}.Core/Models/{Aggregate}Aggregate/Events/{Event}.cs
namespace {AppName}.Core;  // Does not match file location
```

### 2.2 File Organization

- Each file SHALL contain exactly one class, enum, record, or other type definition
- Multiple object definitions in a single file are prohibited

### 2.3 Identity Property Naming

Entity identity properties SHALL include the entity name.

**Compliant:**

```csharp
public Guid CustomerId { get; set; }
```

**Non-Compliant:**

```csharp
public Guid Id { get; set; }
```

### 2.4 Object Mapping

- AutoMapper SHALL NOT be used
- Extension methods with `ToDto()` SHALL be created for mapping Core models to DTOs in the API layer

### 2.5 Implementation Simplicity

All implementations SHALL be as simple as possible. Complex solutions SHALL be avoided in favor of straightforward, maintainable approaches.

### 2.6 Database Configuration

SQL Server Express is the default database for development environments.

---

## 3. Backend Architecture

### 3.1 Microservices Structure

Each microservice SHALL be independently deployable and own its data. Each microservice SHOULD map to a bounded context.

### 3.2 Three-Project Structure

Each microservice SHALL follow a three-project structure:

```
{ServiceName}.Core
{ServiceName}.Infrastructure
{ServiceName}.Api
```

### 3.3 Critical Architectural Constraints

#### No Repository Pattern

**CRITICAL**: The Repository pattern (IRepository or any repository abstraction) SHALL NOT be used for data persistence. All data access SHALL be performed directly through the context interface (e.g., `I{AppName}Context`).

#### Services in Core (Preferred)

Business logic and domain services SHALL be implemented in the Core project whenever possible. Services should only be placed outside Core when they have explicit infrastructure dependencies.

#### Context as Persistence Surface

The context interface is the single, unified persistence surface for the entire system. No additional abstraction layers SHALL be introduced.

### 3.4 Structured Logging

**CRITICAL**: Implement structured logging using Serilog across all backend areas.

| Log Level | Usage |
|-----------|-------|
| Information | Normal operations and API calls |
| Warning | Validation failures and business rule violations |
| Error | All exceptions and external service failures |
| Critical | System failures and data corruption issues |

**Log Enrichment Requirements:**

- CorrelationId
- UserId
- Timestamp
- Relevant context identifiers (e.g., CustomerId, EventId)
- Environment information

**Security**: Sensitive data (passwords, tokens, credit card information, personal identification numbers) SHALL NOT be logged.

---

## 4. Core Project Guidelines

### 4.1 Project Naming

The Core project SHALL be named `{AppName}.Core`.

### 4.2 Aggregate Organization

```
{AppName}.Core/
├── Models/
│   └── {Aggregate}Aggregate/
│       ├── {AggregateRoot}.cs
│       ├── Events/
│       │   └── {Event}.cs
│       ├── Enums/
│       │   └── {Enum}.cs
│       └── Entities/
│           └── {Entity}.cs
├── Services/
│   ├── I{Service}.cs
│   └── {Service}.cs
└── I{AppName}Context.cs
```

### 4.3 Persistence Interface

The Core project SHALL contain an interface (e.g., `I{AppName}Context`) with DbSet properties for each entity. The implementation resides in the Infrastructure project.

### 4.4 Service Layer

- Services SHALL be located in a `Services` folder
- Services SHALL be implemented in `Core/Services` whenever possible (preferred approach)
- Services requiring infrastructure dependencies SHALL be placed in `Infrastructure/Services`

---

## 5. Infrastructure Project Guidelines

### 5.1 Project Naming

The Infrastructure project SHALL be named `{AppName}.Infrastructure`.

### 5.2 Structure

```
{AppName}.Infrastructure/
├── Data/
│   ├── {AppName}Context.cs
│   ├── Migrations/
│   └── Configurations/
│       └── {Entity}Configuration.cs
├── Services/
│   └── {ExternalService}.cs
└── Seeding/
    └── {Entity}Seeder.cs
```

### 5.3 Responsibilities

- Implementation of the context interface
- Entity Framework migrations
- Entity configurations
- Database seeding services
- External service integrations

---

## 6. API Project Guidelines

### 6.1 Project Naming

The API project SHALL be named `{AppName}.Api`.

### 6.2 Structure

```
{AppName}.Api/
├── Controllers/
│   └── {Entity}Controller.cs
├── Features/
│   └── {BoundedContext}/
│       ├── Commands/
│       │   └── {Command}/
│       │       ├── {Command}Handler.cs
│       │       ├── {Command}Request.cs
│       │       └── {Command}Response.cs
│       └── Queries/
│           └── {Query}/
│               ├── {Query}Handler.cs
│               ├── {Query}Request.cs
│               └── {Query}Response.cs
├── Behaviours/
│   └── ValidationBehaviour.cs
└── Program.cs
```

### 6.3 Feature Organization

- Commands and Queries (using MediatR) SHALL be grouped in folders by Bounded Context
- DTOs SHALL be contained within their respective feature folders

### 6.4 CORS Configuration

- A CORS policy SHALL be defined
- Allowed origins SHALL be retrieved from configuration
- Origins SHALL include the URLs where frontends are hosted

### 6.5 JSON Serialization

Configure JSON serialization to handle enums as text strings:

```csharp
using System.Text.Json.Serialization;

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
```

---

## 7. Frontend Guidelines (Angular)

### 7.1 Project Structure

```
src/{AppName}.WebApp/
├── projects/
│   └── {app-name}/
│       └── src/
│           ├── app/
│           │   ├── pages/
│           │   │   └── {page}/
│           │   │       ├── {page}.html
│           │   │       ├── {page}.scss
│           │   │       └── {page}.ts
│           │   └── components/
│           │       └── {component}/
│           │           ├── {component}.html
│           │           ├── {component}.scss
│           │           └── {component}.ts
│           └── e2e/
└── angular.json
```

### 7.2 Technology Stack

| Requirement | Standard |
|-------------|----------|
| Framework | Latest stable Angular version |
| UI Library | Latest stable Angular Material |
| State Management | RxJS (NOT NgRx) |
| Styling | Material 3 guidelines, BEM methodology |

**Prohibited:**

- NgRx for state management
- Angular signals
- Colors not defined in Angular Material theme

### 7.3 Component File Structure

Component files SHALL be separated:

**Compliant:**

```
header.html
header.scss
header.ts
```

**Non-Compliant:**

```
header.component.html
header.component.scss
header.component.ts
```

### 7.4 Component Naming

**Compliant:**

```typescript
export class Header { }
```

**Non-Compliant:**

```typescript
export class HeaderComponent { }
```

### 7.5 Module Exports

Create barrel exports (`index.ts`) for every folder, exporting all TypeScript code except test code.

### 7.6 API Configuration

**CRITICAL**: The `baseUrl` SHALL contain ONLY the base URI without the `/api` path segment.

**Compliant:**

```typescript
// Configuration
baseUrl = "http://localhost:3200"

// Service usage
this.http.get(`${baseUrl}/api/customers`)
```

**Non-Compliant:**

```typescript
// Configuration
baseUrl = "http://localhost:3200/api"

// Service usage
this.http.get(`${baseUrl}/customers`)
```

### 7.7 Reactive Data Loading Pattern

**CRITICAL**: Components SHALL use the async pipe pattern with observables.

**Compliant:**

```typescript
import { Component, inject } from '@angular/core';
import { DataService } from '../data-service';
import { map } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-correct',
  imports: [CommonModule],
  templateUrl: './correct.html',
  styleUrl: './correct.scss',
})
export class Correct {
  _dataService = inject(DataService);

  viewModel$ = this._dataService.get().pipe(
    map(x => ({ message: x }))
  );
}
```

```html
<ng-container *ngIf="viewModel$ | async as vm">
    <h1>{{ vm.message }}</h1>
</ng-container>
```

**Non-Compliant:**

```typescript
export class Incorrect implements OnInit {
  _dataService = inject(DataService);
  message: string = '';

  ngOnInit(): void {
    this._dataService.get().subscribe(data => {
      this.message = data;
    });
  }
}
```

**Pattern Requirements:**

- Expose observables as public properties with `$` suffix
- Use the Angular `async` pipe in templates
- DO NOT manually call `.subscribe()` for data loading
- DO NOT maintain local state properties that duplicate observable data
- Use RxJS operators for transformations

### 7.8 Design Standards

- Mobile-first responsive design
- BEM HTML class naming strategy
- Design tokens for consistent spacing
- Default Angular Material colors and theme only

---

## 8. Messaging and Event-Driven Architecture

### 8.1 Message Envelope Structure

All messages SHALL use a consistent envelope pattern:

```csharp
public class MessageEnvelope<TPayload> where TPayload : IMessage
{
    public MessageHeader Header { get; init; }
    public TPayload Payload { get; init; }
}
```

### 8.2 Message Header Fields

| Field | Type | Purpose |
|-------|------|---------|
| MessageType | string | Type discrimination |
| MessageId | string (GUID) | Idempotency |
| CorrelationId | string (GUID) | Distributed tracing |
| CausationId | string | Event chain tracking |
| TimestampUnixMs | long | Creation time |
| SourceService | string | Origin identification |
| SchemaVersion | int | Version (default: 1) |
| Metadata | Dictionary<string, string> | Extensibility |

### 8.3 Domain Message Markers

```csharp
public interface IMessage { }

public interface IDomainEvent : IMessage
{
    string AggregateId { get; }
    string AggregateType { get; }
}

public interface ICommand : IMessage
{
    string TargetId { get; }
}
```

### 8.4 Serialization

Use MessagePack with LZ4 compression:

```csharp
[MessagePackObject]
[MessageChannel("domain", "aggregate", "event-type", version: 1)]
public sealed class SampleEvent : IDomainEvent
{
    [Key(0)]
    public required string EntityId { get; init; }

    [Key(1)]
    public required string Name { get; init; }

    [IgnoreMember]
    public string AggregateId => EntityId;
    
    [IgnoreMember]
    public string AggregateType => "Sample";
}
```

### 8.5 Schema Evolution Rules

| Change Type | Status | Action |
|-------------|--------|--------|
| Adding new optional fields | SAFE | Use new Key indices |
| Adding enum values at end | SAFE | Allowed |
| Renaming fields (Key preserved) | SAFE | Allowed |
| Changing required to optional | SAFE | Allowed |
| Removing fields | UNSAFE | Mark as [Obsolete] instead |
| Changing field types | UNSAFE | Prohibited |
| Changing Key indices | UNSAFE | Prohibited |
| Reordering enum values | UNSAFE | Prohibited |

### 8.6 Channel Naming

Pattern: `{domain}.{aggregate}.{event-type}.v{version}`

Example: `vehicles.listing.created.v1`

### 8.7 Message Size Limits

- Individual messages SHOULD NOT exceed 1MB uncompressed
- Messages approaching 100KB SHOULD be investigated
- Large payloads SHALL use reference pattern (store in blob storage, send URL)

### 8.8 Subscription Configuration

```csharp
public static ISubscriptionRegistry Configure()
{
    return new SubscriptionRegistryBuilder("service-name")
        .Subscribe<EventType, EventHandler>(opt => 
        {
            opt.MaxConcurrency = 4;
            opt.RetryPolicy = new RetryPolicy { MaxRetries = 3 };
        })
        .Build();
}
```

### 8.9 Message Handler Pattern

```csharp
public sealed class SampleHandler : MessageHandlerBase<SampleEvent>
{
    public override async Task HandleAsync(
        SampleEvent message, 
        MessageContext context, 
        CancellationToken ct)
    {
        // Idempotency check
        var key = $"sample:{message.EntityId}:{context.Header.MessageId}";
        if (!await _idempotency.TryProcessAsync(key, TimeSpan.FromDays(7), ct))
        {
            Logger.LogDebug("Duplicate message {MessageId}, skipping", context.Header.MessageId);
            return;
        }

        // Process message
    }
}
```

### 8.10 Retry Policy

Default configuration:

- MaxRetries: 3
- InitialDelay: 1 second
- BackoffMultiplier: 2.0

Formula: `delay = InitialDelay * (BackoffMultiplier ^ attempt)`

---

## 9. Code Quality and Linting

### 9.1 Backend Linting

**CRITICAL**: Use StyleCop.Analyzers and Microsoft.CodeAnalysis.NetAnalyzers.

- All linting warnings SHALL be treated as errors and block the build
- Use a shared `.editorconfig` file at solution root

**Enforcement:**

- Consistent code style and formatting
- Null safety analysis
- Code quality best practices
- Security vulnerability detection

### 9.2 Frontend Linting

**CRITICAL**: Use ESLint with @angular-eslint plugin.

- All linting errors SHALL block the build
- Add `lint` script to package.json

**Enforcement:**

- TypeScript best practices
- Angular-specific conventions
- Accessibility (a11y) rules
- Import organization

### 9.3 Build Integration

**CRITICAL**: Both backend and frontend builds SHALL fail if any linting errors are detected.

Linting SHALL be integrated into CI/CD pipelines as a mandatory quality gate.

---

## 10. Testing Standards

### 10.1 Backend Testing

- Each message type MUST have round-trip serialization tests
- Schema evolution scenarios MUST be tested
- Large message handling MUST be tested
- Invalid message handling MUST be tested

### 10.2 Frontend Testing

| Test Type | Tool |
|-----------|------|
| Unit Tests | Jest |
| E2E Tests | Playwright |

- Jest tests SHALL be configured to ignore the e2e folder
- E2E folder location: `src/{AppName}.WebApp/projects/{app-name}/src/e2e`

### 10.3 Handler Testing

- Handlers MUST be unit testable without external dependencies
- Idempotency store MUST have in-memory implementation for testing
- Cycle detector MUST have in-memory implementation for testing

---

## Appendix A: Quick Reference

### Project Naming Convention

| Layer | Naming Pattern |
|-------|----------------|
| Core | `{AppName}.Core` |
| Infrastructure | `{AppName}.Infrastructure` |
| API | `{AppName}.Api` |
| Frontend | `{AppName}.WebApp` |

### Prohibited Patterns

| Pattern | Reason |
|---------|--------|
| Repository Pattern | Use context interface directly |
| AutoMapper | Use extension methods with ToDto() |
| NgRx | Use RxJS for state management |
| Angular Signals | Use RxJS observables |
| Manual subscriptions | Use async pipe pattern |
| `Id` property naming | Use `{Entity}Id` |
| Multiple types per file | One type per file |

### Required Patterns

| Pattern | Location |
|---------|----------|
| Structured Logging | All backend services |
| Async Pipe | All Angular components loading data |
| BEM Naming | All frontend CSS classes |
| Barrel Exports | All frontend folders |
| Message Envelope | All inter-service messages |
| Idempotency Checks | All message handlers |

---

**End of Coding Guidelines**
