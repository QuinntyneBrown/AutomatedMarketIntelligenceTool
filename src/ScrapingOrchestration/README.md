# Scraping Orchestration Service

The Scraping Orchestration Service manages and coordinates web scraping operations across multiple vehicle marketplace sources.

## Purpose

This microservice is responsible for:
- Creating and managing scraping jobs
- Coordinating scraping sessions across multiple sources
- Tracking job status and progress
- Managing search parameters and filters
- Providing health monitoring for scraping operations

## Architecture

```
ScrapingOrchestration/
├── ScrapingOrchestration.Api/           # REST API layer
│   └── Controllers/
│       └── ScrapingController.cs
├── ScrapingOrchestration.Core/          # Domain layer
│   ├── Models/
│   │   ├── SearchSession.cs
│   │   └── ScrapingJob.cs
│   └── Services/
│       ├── IJobScheduler.cs
│       └── ISearchSessionService.cs
└── ScrapingOrchestration.Infrastructure/ # Data access layer
```

## API Endpoints

### Jobs
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/scraping/jobs` | Create new scraping job |
| GET | `/api/scraping/jobs/{id}` | Get job status |
| POST | `/api/scraping/jobs/{id}/cancel` | Cancel running job |

### Sessions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/scraping/sessions` | Get all sessions |
| GET | `/api/scraping/sessions/{sessionId}/jobs` | Get jobs for session |

### Health
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/scraping/health` | Get service health status |

## Domain Models

### SearchSession
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Session identifier |
| `Status` | enum | Running, Completed, Failed, Cancelled |
| `Sources` | List | Target marketplaces |
| `CreatedAt` | DateTime | Session creation time |
| `StartedAt` | DateTime | Session start time |
| `CompletedAt` | DateTime | Session completion time |
| `TotalListingsFound` | int | Listings discovered |
| `TotalErrors` | int | Error count |
| `SearchParameters` | object | Search criteria |

### SearchParameters
| Field | Type | Description |
|-------|------|-------------|
| `Make` | string | Vehicle manufacturer |
| `Model` | string | Vehicle model |
| `YearFrom/YearTo` | int | Year range |
| `MinPrice/MaxPrice` | decimal | Price range |
| `MaxMileage` | int | Maximum mileage |
| `PostalCode` | string | Center location |
| `RadiusKm` | int | Search radius |
| `Province` | string | Province filter |
| `MaxResults` | int | Result limit |

### ScrapingJob
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Job identifier |
| `SessionId` | Guid | Parent session |
| `Source` | string | Target marketplace |
| `Status` | enum | Job status |
| `ListingsFound` | int | Listings scraped |
| `PagesCrawled` | int | Pages processed |
| `ErrorMessage` | string | Error details |
| `RetryCount` | int | Retry attempts |

## Integration Events

| Event | Description |
|-------|-------------|
| `ListingScrapedEvent` | Published for each scraped listing |
| `ScrapingJobCreatedEvent` | Published when job is created |
| `ScrapingJobCompletedEvent` | Published when job completes |
| `ScrapingJobFailedEvent` | Published when job fails |

## Integration Points

### Outbound
- **Scraping Worker Service**: Delegates actual scraping work
- **Listing Service**: Sends scraped listings for storage
- **Dashboard Service**: Reports scraping metrics

### Inbound
- **Configuration Service**: Gets throttling settings
- **API Gateway**: Routes scraping requests

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `scraping-job-creation.puml` - Job creation and execution flow
- `session-management.puml` - Session lifecycle flow
