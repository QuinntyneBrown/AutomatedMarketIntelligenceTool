# Configuration Service

The Configuration Service provides centralized configuration management, feature flags, and resource throttling for all platform services.

## Purpose

This microservice is responsible for:
- Managing service-level configurations
- Feature flag management
- Resource throttling controls
- Configuration versioning
- Runtime configuration updates

## Architecture

```
Configuration/
├── Configuration.Api/           # REST API layer
│   └── Controllers/
│       └── ConfigController.cs
├── Configuration.Core/          # Domain layer
│   └── Models/
│       ├── ServiceConfiguration.cs
│       ├── FeatureFlag.cs
│       └── ResourceThrottle.cs
└── Configuration.Infrastructure/ # Data access layer
```

## API Endpoints

### Service Configuration
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/config/{service}` | Get service config |
| GET | `/api/config/{service}/{key}` | Get specific key |
| PUT | `/api/config/{service}` | Set configuration |
| PUT | `/api/config/{service}/bulk` | Bulk update |
| DELETE | `/api/config/{service}/{key}` | Delete key |

### Feature Flags
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/config/features` | List all flags |
| GET | `/api/config/features/{flag}` | Get flag status |
| POST | `/api/config/features` | Create flag |
| PUT | `/api/config/features/{flag}` | Update flag |
| POST | `/api/config/features/{flag}/toggle` | Toggle flag |
| DELETE | `/api/config/features/{flag}` | Delete flag |

### Resource Throttling
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/config/throttle` | List throttles |
| GET | `/api/config/throttle/{resource}` | Get throttle |
| POST | `/api/config/throttle` | Create throttle |
| PUT | `/api/config/throttle/{resource}` | Update throttle |
| DELETE | `/api/config/throttle/{resource}` | Delete throttle |

## Domain Models

### ServiceConfiguration
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Config identifier |
| `ServiceName` | string | Target service |
| `Key` | string | Configuration key |
| `Value` | string | Configuration value |
| `Version` | int | Config version |
| `UpdatedAt` | DateTime | Last update |

### FeatureFlag
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Flag identifier |
| `Name` | string | Flag name |
| `IsEnabled` | bool | Flag status |
| `Description` | string | Flag description |
| `UpdatedAt` | DateTime | Last update |

### ResourceThrottle
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Throttle identifier |
| `ResourceName` | string | Resource name |
| `MaxConcurrent` | int | Max concurrent ops |
| `RateLimitPerMinute` | int | Requests per minute |
| `IsEnabled` | bool | Throttle active |

## Feature Flags

Common feature flags:
| Flag | Description |
|------|-------------|
| `EnableNewScraper` | Enable new scraper implementation |
| `EnableAdvancedDedup` | Advanced deduplication algorithm |
| `EnableEmailAlerts` | Email notification system |
| `MaintenanceMode` | System maintenance mode |

## Service Configurations

Example configurations:
| Service | Key | Description |
|---------|-----|-------------|
| `Scraping` | `MaxConcurrentJobs` | Concurrent scraping jobs |
| `Scraping` | `RateLimitPerSource` | Requests per source |
| `Image` | `MaxImageSize` | Max image file size |
| `Search` | `MaxResultsPerPage` | Search result limit |

## Resource Throttles

Throttle configuration:
| Resource | MaxConcurrent | RateLimit |
|----------|---------------|-----------|
| `Kijiji` | 5 | 60/min |
| `AutoTrader` | 3 | 30/min |
| `ImageProcessing` | 10 | 100/min |
| `ReportGeneration` | 2 | 10/min |

## Integration Points

### Outbound
- **All Services**: Provides configuration data
- No inbound dependencies (standalone service)

## Configuration Flow

1. Service starts up
2. Requests configuration from this service
3. Caches configuration locally
4. Polls for updates periodically
5. Applies new configuration at runtime

## Versioning

Configuration versioning:
- Each update increments version
- Services can track applied version
- Rollback to previous versions
- Audit trail of changes

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `config-retrieval.puml` - Configuration fetch flow
- `feature-flag-toggle.puml` - Feature flag toggle flow
