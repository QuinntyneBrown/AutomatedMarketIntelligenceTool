# Microservices Architecture for AutomatedMarketIntelligenceTool

This document outlines the microservices architecture that would be needed if the AutomatedMarketIntelligenceTool was built using a microservices approach.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              API Gateway                                      │
│                    (Authentication, Routing, Rate Limiting)                   │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
        ┌─────────────────────────────┼─────────────────────────────┐
        │                             │                             │
        ▼                             ▼                             ▼
┌───────────────┐           ┌───────────────┐           ┌───────────────┐
│   Scraping    │           │    Search     │           │   Reporting   │
│   Service     │           │   Service     │           │   Service     │
└───────────────┘           └───────────────┘           └───────────────┘
        │                             │                             │
        ▼                             ▼                             ▼
┌───────────────┐           ┌───────────────┐           ┌───────────────┐
│ Deduplication │           │    Alert      │           │   Dashboard   │
│   Service     │           │   Service     │           │   Service     │
└───────────────┘           └───────────────┘           └───────────────┘
        │                             │                             │
        └─────────────────────────────┼─────────────────────────────┘
                                      │
                                      ▼
                          ┌───────────────────┐
                          │   Message Broker  │
                          │  (RabbitMQ/Kafka) │
                          └───────────────────┘
```

---

## Core Microservices

### 1. API Gateway Service

**Purpose:** Central entry point for all client requests, handling authentication, routing, and rate limiting.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Request routing, JWT authentication, rate limiting, request/response logging, SSL termination |
| **Technology** | Ocelot, YARP, or Kong |
| **Dependencies** | Identity Service |
| **Database** | Redis (for rate limiting cache) |
| **Endpoints** | Routes to all downstream services |

---

### 2. Identity Service

**Purpose:** Manages user authentication and authorization.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | User registration/login, JWT token generation, role management, API key management |
| **Technology** | ASP.NET Core Identity, IdentityServer4/Duende |
| **Dependencies** | None (foundational service) |
| **Database** | PostgreSQL/SQL Server |
| **Events Published** | `UserRegistered`, `UserAuthenticated` |

**Key Endpoints:**
- `POST /auth/login` - User authentication
- `POST /auth/register` - User registration
- `GET /users/{id}` - Get user details
- `POST /api-keys` - Generate API key

---

### 3. Scraping Orchestration Service

**Purpose:** Coordinates and manages scraping jobs across multiple worker instances.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Job scheduling, worker coordination, session management, scraping status tracking, health monitoring |
| **Technology** | ASP.NET Core, Hangfire/Quartz.NET |
| **Dependencies** | Identity Service, Message Broker |
| **Database** | PostgreSQL (job metadata, sessions) |
| **Events Published** | `ScrapingJobCreated`, `ScrapingJobCompleted`, `ScrapingJobFailed` |
| **Events Consumed** | `ListingScraped`, `WorkerHealthUpdate` |

**Key Endpoints:**
- `POST /scraping/jobs` - Create scraping job
- `GET /scraping/jobs/{id}` - Get job status
- `POST /scraping/jobs/{id}/cancel` - Cancel job
- `GET /scraping/sessions` - List active sessions
- `GET /scraping/health` - Worker health status

---

### 4. Scraping Worker Service

**Purpose:** Executes actual web scraping operations against automotive listing websites.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Browser automation, page parsing, proxy rotation, user-agent management, rate limiting, bot detection handling |
| **Technology** | .NET Worker Service, Playwright |
| **Dependencies** | Scraping Orchestration Service, Message Broker |
| **Database** | Redis (response caching) |
| **Events Published** | `ListingScraped`, `ScrapingError`, `BotDetected` |
| **Events Consumed** | `ScrapingJobCreated` |
| **Scaling** | Horizontal (multiple worker instances) |

**Supported Scrapers:**
- Autotrader.ca Scraper
- Kijiji.ca Scraper
- CarGurus Scraper
- Clutch.ca Scraper
- Auto123 Scraper
- CarFax Scraper
- CarMax Scraper
- Carvana Scraper
- TrueCar Scraper
- Vroom Scraper
- Tabangimotors Scraper

---

### 5. Listing Service

**Purpose:** Central repository for all car listings with CRUD operations.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Listing storage, retrieval, updates, price history tracking, listing lifecycle management |
| **Technology** | ASP.NET Core Web API |
| **Dependencies** | Identity Service, Deduplication Service |
| **Database** | PostgreSQL (primary), Elasticsearch (search index) |
| **Events Published** | `ListingCreated`, `ListingUpdated`, `ListingDeleted`, `PriceChanged` |
| **Events Consumed** | `ListingScraped`, `DeduplicationCompleted` |

**Key Endpoints:**
- `GET /listings` - Search listings with filters
- `GET /listings/{id}` - Get listing details
- `POST /listings` - Create listing
- `PUT /listings/{id}` - Update listing
- `GET /listings/{id}/price-history` - Price history
- `POST /listings/batch` - Batch import

---

### 6. Deduplication Service

**Purpose:** Identifies and links duplicate listings across different sources.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | VIN matching, fuzzy matching, image similarity detection, confidence scoring, manual review queue |
| **Technology** | ASP.NET Core, ML.NET (optional) |
| **Dependencies** | Listing Service, Image Service |
| **Database** | PostgreSQL (dedup config, audit logs), Redis (matching cache) |
| **Events Published** | `DeduplicationCompleted`, `DuplicateFound`, `ReviewRequired` |
| **Events Consumed** | `ListingCreated`, `ListingUpdated` |

**Matching Algorithms:**
- VIN-based exact matching (100% confidence)
- Partial VIN matching (95% confidence)
- ExternalId + SourceSite matching
- Fuzzy logic (make/model/year/price/mileage/location)
- Image perceptual hash similarity

**Key Endpoints:**
- `POST /deduplication/analyze` - Analyze listing for duplicates
- `GET /deduplication/config` - Get dedup configuration
- `PUT /deduplication/config` - Update thresholds
- `GET /deduplication/audit` - Audit trail
- `GET /deduplication/review-queue` - Manual review items
- `POST /deduplication/review/{id}/resolve` - Resolve review item

---

### 7. Vehicle Aggregation Service

**Purpose:** Manages deduplicated vehicle entities that link multiple listings.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Vehicle entity management, best price calculation, listing aggregation, cross-source analysis |
| **Technology** | ASP.NET Core Web API |
| **Dependencies** | Listing Service, Deduplication Service |
| **Database** | PostgreSQL |
| **Events Published** | `VehicleCreated`, `VehicleUpdated`, `BestPriceChanged` |
| **Events Consumed** | `DeduplicationCompleted`, `ListingUpdated` |

**Key Endpoints:**
- `GET /vehicles` - Search vehicles
- `GET /vehicles/{id}` - Get vehicle with linked listings
- `GET /vehicles/{id}/listings` - All listings for a vehicle
- `GET /vehicles/{id}/price-comparison` - Cross-source price comparison

---

### 8. Search Service

**Purpose:** Provides fast, full-text search capabilities across listings.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Full-text search, faceted filtering, autocomplete, search profiles, geographic search |
| **Technology** | ASP.NET Core, Elasticsearch |
| **Dependencies** | Listing Service |
| **Database** | Elasticsearch (search index), PostgreSQL (profiles) |
| **Events Consumed** | `ListingCreated`, `ListingUpdated`, `ListingDeleted` |

**Key Endpoints:**
- `POST /search` - Execute search with filters
- `GET /search/autocomplete` - Autocomplete suggestions
- `GET /search/profiles` - List search profiles
- `POST /search/profiles` - Create search profile
- `GET /search/facets` - Get available facets

---

### 9. Alert Service

**Purpose:** Manages price and inventory alerts with notification delivery.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Alert creation, condition matching, notification dispatch, alert history |
| **Technology** | ASP.NET Core, Background Workers |
| **Dependencies** | Listing Service, Notification Service |
| **Database** | PostgreSQL |
| **Events Published** | `AlertTriggered`, `AlertCreated` |
| **Events Consumed** | `ListingCreated`, `PriceChanged` |

**Alert Types:**
- Price drop alerts
- Price threshold alerts
- New listing alerts (matching criteria)
- Inventory change alerts

**Key Endpoints:**
- `GET /alerts` - List user alerts
- `POST /alerts` - Create alert
- `PUT /alerts/{id}` - Update alert
- `DELETE /alerts/{id}` - Delete alert
- `GET /alerts/{id}/history` - Alert trigger history

---

### 10. Notification Service

**Purpose:** Handles multi-channel notification delivery.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Email sending, webhook dispatch, push notifications, notification templates, delivery tracking |
| **Technology** | ASP.NET Core, SendGrid/AWS SES |
| **Dependencies** | Identity Service |
| **Database** | PostgreSQL (notification logs) |
| **Events Consumed** | `AlertTriggered`, `ReportGenerated` |

**Notification Channels:**
- Email
- Webhooks
- Push notifications (future)
- SMS (future)

**Key Endpoints:**
- `POST /notifications/send` - Send notification
- `GET /notifications/templates` - List templates
- `POST /notifications/webhooks` - Register webhook
- `GET /notifications/history` - Delivery history

---

### 11. Reporting Service

**Purpose:** Generates reports in multiple formats.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Report generation, template management, scheduled reports, format conversion |
| **Technology** | ASP.NET Core, Scriban, PDFsharp, ClosedXML |
| **Dependencies** | Listing Service, Vehicle Service |
| **Database** | PostgreSQL (report metadata), Blob Storage (generated files) |
| **Events Published** | `ReportGenerated`, `ReportFailed` |

**Supported Formats:**
- JSON
- CSV
- HTML
- PDF
- Excel (XLSX)

**Key Endpoints:**
- `POST /reports/generate` - Generate report
- `GET /reports/{id}` - Get report status/download
- `GET /reports/templates` - List templates
- `POST /reports/schedule` - Schedule recurring report
- `GET /reports/scheduled` - List scheduled reports

---

### 12. Dashboard & Analytics Service

**Purpose:** Provides real-time analytics and market intelligence.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Market statistics, trend analysis, dealer analytics, relisting detection, real-time metrics |
| **Technology** | ASP.NET Core, SignalR (real-time) |
| **Dependencies** | Listing Service, Vehicle Service, Scraping Service |
| **Database** | PostgreSQL, Redis (real-time metrics cache) |
| **Events Consumed** | `ListingCreated`, `ScrapingJobCompleted`, `PriceChanged` |

**Key Endpoints:**
- `GET /dashboard/overview` - Dashboard summary
- `GET /dashboard/market-trends` - Market trend data
- `GET /dashboard/scraping-status` - Active scraping status
- `GET /dashboard/health` - System health metrics
- `WS /dashboard/realtime` - WebSocket for real-time updates

---

### 13. Dealer Service

**Purpose:** Manages dealer/seller information with normalization.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Dealer normalization, reliability scoring, inventory tracking, dealer deduplication |
| **Technology** | ASP.NET Core Web API |
| **Dependencies** | Listing Service |
| **Database** | PostgreSQL |
| **Events Published** | `DealerCreated`, `DealerUpdated`, `ReliabilityScoreChanged` |
| **Events Consumed** | `ListingCreated` |

**Key Endpoints:**
- `GET /dealers` - Search dealers
- `GET /dealers/{id}` - Get dealer details
- `GET /dealers/{id}/inventory` - Dealer's current inventory
- `GET /dealers/{id}/reliability` - Reliability metrics
- `GET /dealers/{id}/relisted` - Relisted vehicle detection

---

### 14. Image Service

**Purpose:** Handles image downloading, processing, and similarity detection.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Image downloading, perceptual hashing, similarity comparison, image storage |
| **Technology** | ASP.NET Core, ImageSharp |
| **Dependencies** | None |
| **Database** | PostgreSQL (hash index), Blob Storage (images) |
| **Events Published** | `ImageProcessed`, `ImageHashCalculated` |

**Key Endpoints:**
- `POST /images/upload` - Upload image
- `POST /images/hash` - Calculate perceptual hash
- `POST /images/compare` - Compare image similarity
- `GET /images/{id}` - Retrieve image

---

### 15. Watch List Service

**Purpose:** Manages user watch lists for tracking specific vehicles.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Watch list management, change tracking, watch list notifications |
| **Technology** | ASP.NET Core Web API |
| **Dependencies** | Listing Service, Alert Service |
| **Database** | PostgreSQL |
| **Events Consumed** | `ListingUpdated`, `PriceChanged` |

**Key Endpoints:**
- `GET /watchlist` - Get user's watch list
- `POST /watchlist` - Add listing to watch list
- `DELETE /watchlist/{listingId}` - Remove from watch list
- `GET /watchlist/changes` - Recent changes to watched items

---

### 16. Geographic/Market Service

**Purpose:** Manages custom market regions and geographic calculations.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Custom market definitions, postal code lookup, geo-distance calculations, market boundaries |
| **Technology** | ASP.NET Core Web API |
| **Dependencies** | None |
| **Database** | PostgreSQL with PostGIS |

**Key Endpoints:**
- `GET /markets` - List custom markets
- `POST /markets` - Create custom market
- `PUT /markets/{id}` - Update market
- `POST /markets/geocode` - Geocode postal code
- `POST /markets/distance` - Calculate distance between points
- `POST /markets/within` - Check if location is within market

---

### 17. Configuration Service

**Purpose:** Centralized configuration management for all services.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Service configuration, feature flags, user-specific settings, configuration versioning |
| **Technology** | ASP.NET Core, Consul/etcd |
| **Dependencies** | Identity Service |
| **Database** | Consul/etcd or PostgreSQL |

**Key Endpoints:**
- `GET /config/{service}` - Get service configuration
- `PUT /config/{service}` - Update configuration
- `GET /config/features` - Feature flags
- `PUT /config/features/{flag}` - Toggle feature flag

---

### 18. Backup & Data Management Service

**Purpose:** Handles data backup, restore, and import operations.

| Attribute | Details |
|-----------|---------|
| **Responsibilities** | Database backups, data restoration, batch imports, data export |
| **Technology** | ASP.NET Core Worker Service |
| **Dependencies** | All data services |
| **Database** | Blob Storage (backups) |
| **Events Published** | `BackupCompleted`, `RestoreCompleted`, `ImportCompleted` |

**Key Endpoints:**
- `POST /backup` - Create backup
- `GET /backups` - List backups
- `POST /restore/{backupId}` - Restore from backup
- `POST /import` - Batch data import
- `GET /import/{jobId}` - Import job status

---

## Supporting Infrastructure

### Message Broker (RabbitMQ/Kafka)

**Purpose:** Asynchronous communication between services.

**Key Exchanges/Topics:**
- `scraping.events` - Scraping job lifecycle events
- `listing.events` - Listing CRUD events
- `deduplication.events` - Deduplication results
- `alert.events` - Alert triggers
- `notification.events` - Notification requests

---

### Service Registry (Consul/Eureka)

**Purpose:** Service discovery and health checking.

**Registered Services:**
- All microservices register on startup
- Health check endpoints monitored
- Automatic deregistration on failure

---

### Distributed Cache (Redis)

**Purpose:** Shared caching layer across services.

**Cache Uses:**
- Response caching (scraping)
- Rate limiting tokens
- Session data
- Real-time metrics
- Deduplication matching cache

---

### Blob Storage (S3/Azure Blob/MinIO)

**Purpose:** Store large binary objects.

**Stored Objects:**
- Generated reports (PDF, Excel)
- Cached images
- Database backups
- Import files

---

### Logging & Monitoring (ELK/Grafana)

**Purpose:** Centralized logging and monitoring.

**Components:**
- Elasticsearch - Log storage
- Logstash/Fluentd - Log aggregation
- Kibana - Log visualization
- Prometheus - Metrics collection
- Grafana - Metrics dashboards
- Jaeger/Zipkin - Distributed tracing

---

## Event Catalog

| Event | Publisher | Consumers |
|-------|-----------|-----------|
| `UserRegistered` | Identity Service | Notification Service |
| `ScrapingJobCreated` | Scraping Orchestration | Scraping Workers |
| `ScrapingJobCompleted` | Scraping Orchestration | Dashboard Service |
| `ListingScraped` | Scraping Worker | Listing Service |
| `ListingCreated` | Listing Service | Deduplication, Search, Alert, Dashboard |
| `ListingUpdated` | Listing Service | Deduplication, Search, Alert, Watch List |
| `PriceChanged` | Listing Service | Alert Service, Watch List Service |
| `DeduplicationCompleted` | Deduplication Service | Listing Service, Vehicle Service |
| `DuplicateFound` | Deduplication Service | Vehicle Service |
| `ReviewRequired` | Deduplication Service | Dashboard Service |
| `AlertTriggered` | Alert Service | Notification Service |
| `ReportGenerated` | Reporting Service | Notification Service |
| `ImageProcessed` | Image Service | Deduplication Service |

---

## Deployment Considerations

### Container Orchestration (Kubernetes)

```yaml
# Example deployment structure
namespaces:
  - amit-core        # Core business services
  - amit-scraping    # Scraping workers (scalable)
  - amit-data        # Data services
  - amit-infra       # Infrastructure services

scaling:
  scraping-worker:
    min: 2
    max: 20
    metric: queue-depth

  search-service:
    min: 2
    max: 10
    metric: cpu-utilization

  listing-service:
    min: 3
    max: 15
    metric: request-rate
```

### Database per Service Pattern

| Service | Database | Type |
|---------|----------|------|
| Identity Service | identity_db | PostgreSQL |
| Listing Service | listing_db | PostgreSQL + Elasticsearch |
| Deduplication Service | dedup_db | PostgreSQL |
| Vehicle Service | vehicle_db | PostgreSQL |
| Alert Service | alert_db | PostgreSQL |
| Reporting Service | report_db | PostgreSQL |
| Geographic Service | geo_db | PostgreSQL + PostGIS |

---

## Service Communication Patterns

### Synchronous (HTTP/gRPC)
- API Gateway → All services
- Search Service → Listing Service (queries)
- Dashboard Service → Multiple services (aggregation)

### Asynchronous (Message Queue)
- Scraping Worker → Listing Service (new listings)
- Listing Service → Deduplication Service (analyze)
- Alert Service → Notification Service (send)
- All services → Logging infrastructure

### Event Sourcing (Optional)
- Listing price history
- Deduplication audit trail
- Alert trigger history

---

## Security Considerations

1. **Service-to-Service Authentication:** mTLS between services
2. **API Authentication:** JWT tokens via Identity Service
3. **Rate Limiting:** Per-user and per-endpoint limits
4. **Secrets Management:** HashiCorp Vault or Kubernetes Secrets
5. **Network Policies:** Restrict inter-service communication

---

## Summary

This microservices architecture decomposes the AutomatedMarketIntelligenceTool into **18 distinct services**:

| # | Service | Primary Responsibility |
|---|---------|----------------------|
| 1 | API Gateway | Request routing, authentication |
| 2 | Identity | User management, authentication |
| 3 | Scraping Orchestration | Job coordination |
| 4 | Scraping Worker | Web scraping execution |
| 5 | Listing | Listing data management |
| 6 | Deduplication | Duplicate detection |
| 7 | Vehicle Aggregation | Deduplicated vehicle entities |
| 8 | Search | Full-text search |
| 9 | Alert | Price/inventory alerts |
| 10 | Notification | Multi-channel notifications |
| 11 | Reporting | Report generation |
| 12 | Dashboard & Analytics | Real-time analytics |
| 13 | Dealer | Dealer management |
| 14 | Image | Image processing |
| 15 | Watch List | User watch lists |
| 16 | Geographic/Market | Custom market regions |
| 17 | Configuration | Centralized config |
| 18 | Backup & Data Management | Data operations |

This architecture enables:
- **Independent scaling** of scraping workers based on demand
- **Technology flexibility** per service
- **Fault isolation** preventing cascading failures
- **Team autonomy** with clear service boundaries
- **Easier maintenance** with smaller, focused codebases
