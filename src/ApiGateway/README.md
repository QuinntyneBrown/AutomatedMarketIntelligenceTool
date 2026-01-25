# API Gateway

The API Gateway serves as the central entry point for all client requests, providing routing, authentication, rate limiting, and health aggregation using YARP reverse proxy.

## Purpose

This microservice is responsible for:
- Routing requests to appropriate services
- Centralizing authentication/authorization
- Rate limiting and throttling
- CORS policy management
- Health check aggregation
- Request/response transformation

## Architecture

```
ApiGateway/
├── ApiGateway/              # Main gateway application
│   ├── Program.cs           # YARP configuration
│   └── appsettings.json     # Route configuration
```

## Technology

Built with **YARP (Yet Another Reverse Proxy)**:
- High-performance .NET reverse proxy
- Configuration-based routing
- Middleware pipeline support
- Health check integration

## Service Routes

| Route Prefix | Service | Port |
|--------------|---------|------|
| `/api/auth/*` | Identity Service | 5001 |
| `/api/listings/*` | Listing Service | 5002 |
| `/api/vehicles/*` | Vehicle Aggregation | 5003 |
| `/api/search/*` | Search Service | 5004 |
| `/api/alerts/*` | Alert Service | 5005 |
| `/api/watchlist/*` | WatchList Service | 5006 |
| `/api/reports/*` | Reporting Service | 5007 |
| `/api/dashboard/*` | Dashboard Service | 5008 |
| `/api/dealers/*` | Dealer Service | 5009 |
| `/api/images/*` | Image Service | 5010 |
| `/api/scraping/*` | Scraping Orchestration | 5011 |
| `/api/markets/*` | Geographic Service | 5012 |
| `/api/notifications/*` | Notification Service | 5013 |
| `/api/config/*` | Configuration Service | 5014 |
| `/api/deduplication/*` | Deduplication Service | 5015 |

## Endpoints

### Health
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Basic gateway health |
| GET | `/health/services` | All services health |

## Features

### Rate Limiting
IP-based rate limiting:
- Configurable limits per endpoint
- Sliding window algorithm
- Custom response on limit exceeded

### CORS Policies
| Policy | Description |
|--------|-------------|
| `AllowAll` | Development - all origins |
| `Production` | Production - specific origins |

### Authentication
- JWT token validation
- API key authentication
- Pass-through to services

### Load Balancing
- Round-robin distribution
- Health-aware routing
- Sticky sessions (optional)

## Configuration

### Route Configuration
```json
{
  "ReverseProxy": {
    "Routes": {
      "identity-route": {
        "ClusterId": "identity-cluster",
        "Match": { "Path": "/api/auth/{**catch-all}" }
      }
    },
    "Clusters": {
      "identity-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://localhost:5001" }
        }
      }
    }
  }
}
```

### Rate Limit Configuration
```json
{
  "RateLimiting": {
    "EnableRateLimiting": true,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

## Integration Points

### Inbound
- **External Clients**: Web, Mobile, API consumers
- **UI Frontend**: Admin and user interfaces

### Outbound
- **All Microservices**: Routes requests
- **Identity Service**: Token validation

## Health Aggregation

Aggregates health from all services:
| Status | Condition |
|--------|-----------|
| Healthy | All services healthy |
| Degraded | Some services degraded |
| Unhealthy | Critical service down |

## Error Handling

| Status | Response |
|--------|----------|
| 429 | Rate limit exceeded |
| 502 | Service unavailable |
| 503 | Circuit breaker open |
| 504 | Service timeout |

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `request-routing.puml` - Request routing flow
- `health-aggregation.puml` - Health check flow
