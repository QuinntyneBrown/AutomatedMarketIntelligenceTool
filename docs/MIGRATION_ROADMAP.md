# Microservices Migration Roadmap

This document provides a comprehensive, itemized roadmap for porting the existing monolithic AutomatedMarketIntelligenceTool codebase to the microservices architecture.

---

## Table of Contents

1. [Pre-Migration Preparation](#phase-0-pre-migration-preparation)
2. [Phase 1: Shared Infrastructure](#phase-1-shared-infrastructure)
3. [Phase 2: Identity Service](#phase-2-identity-service)
4. [Phase 3: Image Service](#phase-3-image-service)
5. [Phase 4: Scraping Services](#phase-4-scraping-services)
6. [Phase 5: Listing Service](#phase-5-listing-service)
7. [Phase 6: Deduplication Service](#phase-6-deduplication-service)
8. [Phase 7: Vehicle Aggregation Service](#phase-7-vehicle-aggregation-service)
9. [Phase 8: Search Service](#phase-8-search-service)
10. [Phase 9: Dealer Service](#phase-9-dealer-service)
11. [Phase 10: Alert Service](#phase-10-alert-service)
12. [Phase 11: WatchList Service](#phase-11-watchlist-service)
13. [Phase 12: Notification Service](#phase-12-notification-service)
14. [Phase 13: Reporting Service](#phase-13-reporting-service)
15. [Phase 14: Dashboard Service](#phase-14-dashboard-service)
16. [Phase 15: Geographic Service](#phase-15-geographic-service)
17. [Phase 16: Configuration Service](#phase-16-configuration-service)
18. [Phase 17: Backup Service](#phase-17-backup-service)
19. [Phase 18: API Gateway](#phase-18-api-gateway)
20. [Phase 19: Cleanup & Deletion](#phase-19-cleanup--deletion)
21. [Phase 20: Final Validation](#phase-20-final-validation)

---

## Phase 0: Pre-Migration Preparation

### 0.1 Infrastructure Setup
- [ ] Set up message broker (RabbitMQ or Kafka)
- [ ] Set up Redis for distributed caching
- [ ] Set up service registry (Consul or similar)
- [ ] Set up centralized logging (ELK stack or similar)
- [ ] Set up container registry (Docker Hub, ACR, or ECR)
- [ ] Set up Kubernetes cluster or container orchestration platform
- [ ] Set up CI/CD pipelines for microservices

### 0.2 Shared Libraries Preparation
- [ ] Create `src/Shared/Shared.Contracts` project for shared DTOs and events
- [ ] Create `src/Shared/Shared.Messaging` project for message bus abstractions
- [ ] Create `src/Shared/Shared.ServiceDefaults` project for common service configuration
- [ ] Add shared projects to solution

### 0.3 Database Strategy
- [ ] Plan database-per-service migrations
- [ ] Set up PostgreSQL instances for each service domain
- [ ] Plan data migration scripts for splitting monolithic database
- [ ] Document foreign key relationships that will become service-to-service calls

### 0.4 Remove Multi-Tenancy Code
- [ ] Remove `TenantId` property from all entity models
- [ ] Remove tenant query filters from DbContext configurations
- [ ] Remove tenant-related middleware and services
- [ ] Update all repository queries to remove tenant filtering
- [ ] Remove tenant-related configuration settings

---

## Phase 1: Shared Infrastructure

### 1.1 Shared.Contracts
- [ ] Create base event interfaces (`IIntegrationEvent`, `IDomainEvent`)
- [ ] Create common DTOs for inter-service communication
- [ ] Create shared enums:
  - [ ] `BodyStyle.cs`
  - [ ] `Condition.cs`
  - [ ] `Drivetrain.cs`
  - [ ] `FuelType.cs`
  - [ ] `Transmission.cs`
  - [ ] `SellerType.cs`
  - [ ] `CanadianProvince.cs`
- [ ] Create shared value objects (e.g., `Money`, `VIN`, `PostalCode`)
- [ ] Add unit tests for shared contracts

### 1.2 Shared.Messaging
- [ ] Create message bus abstraction (`IMessageBus`, `IEventPublisher`, `IEventSubscriber`)
- [ ] Implement RabbitMQ adapter
- [ ] Create retry policies and dead letter queue handling
- [ ] Create correlation ID propagation middleware
- [ ] Add integration tests for messaging

### 1.3 Shared.ServiceDefaults
- [ ] Create standard ASP.NET Core configuration
- [ ] Create health check endpoints
- [ ] Create Serilog logging configuration
- [ ] Create OpenTelemetry tracing configuration
- [ ] Create common middleware (error handling, request logging)
- [ ] Create HTTP client factory with resilience policies

### 1.4 Update Solution Structure
- [ ] Add all Shared projects to solution
- [ ] Configure NuGet packaging for shared libraries (if needed)
- [ ] Document shared library usage patterns

---

## Phase 2: Identity Service

### 2.1 Domain Layer (Identity.Core)
- [ ] Create `User` aggregate from existing models
- [ ] Create `Role` entity
- [ ] Create `ApiKey` entity
- [ ] Create domain events:
  - [ ] `UserRegisteredEvent`
  - [ ] `UserAuthenticatedEvent`
  - [ ] `ApiKeyGeneratedEvent`
- [ ] Create domain services for password hashing and validation
- [ ] Add unit tests for domain logic

### 2.2 Infrastructure Layer (Identity.Infrastructure)
- [ ] Create `IdentityDbContext` with User, Role, ApiKey entities
- [ ] Create entity configurations
- [ ] Create EF Core migrations
- [ ] Implement JWT token generation service
- [ ] Implement refresh token service
- [ ] Add repository implementations (if using repository pattern)
- [ ] Add integration tests for data access

### 2.3 API Layer (Identity.Api)
- [ ] Configure ASP.NET Core Identity
- [ ] Implement authentication endpoints:
  - [ ] `POST /auth/register`
  - [ ] `POST /auth/login`
  - [ ] `POST /auth/refresh`
  - [ ] `POST /auth/logout`
- [ ] Implement user management endpoints:
  - [ ] `GET /users/{id}`
  - [ ] `PUT /users/{id}`
  - [ ] `DELETE /users/{id}`
- [ ] Implement API key endpoints:
  - [ ] `POST /api-keys`
  - [ ] `GET /api-keys`
  - [ ] `DELETE /api-keys/{id}`
- [ ] Add Swagger/OpenAPI documentation
- [ ] Add API integration tests

### 2.4 Tests
- [ ] Migrate relevant tests from `AutomatedMarketIntelligenceTool.Core.Tests`
- [ ] Add Identity.Tests unit tests
- [ ] Add Identity.Tests integration tests

---

## Phase 3: Image Service

### 3.1 Domain Layer (Image.Core)
- [ ] Migrate `IImageHashingService` interface from Core
- [ ] Migrate `IImageDownloadService` interface from Core
- [ ] Create `ImageHash` value object
- [ ] Create domain events:
  - [ ] `ImageProcessedEvent`
  - [ ] `ImageHashCalculatedEvent`
- [ ] Add unit tests

### 3.2 Infrastructure Layer (Image.Infrastructure)
- [ ] Migrate `ImageHashingService` from Infrastructure
- [ ] Migrate `ImageDownloadService` from Infrastructure
- [ ] Migrate `PerceptualHashCalculator` from Infrastructure
- [ ] Create `ImageDbContext` for hash storage
- [ ] Configure blob storage for image caching
- [ ] Add SixLabors.ImageSharp dependency
- [ ] Add integration tests

### 3.3 API Layer (Image.Api)
- [ ] Implement endpoints:
  - [ ] `POST /images/upload`
  - [ ] `POST /images/hash`
  - [ ] `POST /images/compare`
  - [ ] `GET /images/{id}`
- [ ] Add Swagger documentation
- [ ] Add API tests

### 3.4 Event Publishing
- [ ] Publish `ImageProcessedEvent` after processing
- [ ] Publish `ImageHashCalculatedEvent` after hash calculation

---

## Phase 4: Scraping Services

### 4.1 Scraping Orchestration - Domain (ScrapingOrchestration.Core)
- [ ] Migrate `SearchSession` aggregate from Core
- [ ] Migrate `SearchSessionStatus` enum from Core
- [ ] Migrate `SearchParameters` from Infrastructure
- [ ] Create domain events:
  - [ ] `ScrapingJobCreatedEvent`
  - [ ] `ScrapingJobCompletedEvent`
  - [ ] `ScrapingJobFailedEvent`
- [ ] Create job scheduling interfaces
- [ ] Add unit tests

### 4.2 Scraping Orchestration - Infrastructure (ScrapingOrchestration.Infrastructure)
- [ ] Create `ScrapingOrchestrationDbContext`
- [ ] Migrate session tracking logic
- [ ] Integrate with message broker for job distribution
- [ ] Implement job scheduling (Hangfire or Quartz.NET)
- [ ] Add integration tests

### 4.3 Scraping Orchestration - API (ScrapingOrchestration.Api)
- [ ] Implement endpoints:
  - [ ] `POST /scraping/jobs`
  - [ ] `GET /scraping/jobs/{id}`
  - [ ] `POST /scraping/jobs/{id}/cancel`
  - [ ] `GET /scraping/sessions`
  - [ ] `GET /scraping/health`
- [ ] Add Swagger documentation
- [ ] Add API tests

### 4.4 Scraping Worker - Domain (ScrapingWorker.Core)
- [ ] Migrate `ISiteScraper` interface from Infrastructure
- [ ] Migrate `ScrapedListing` aggregate from Core
- [ ] Migrate `ScrapeProgress` models from Infrastructure
- [ ] Create scraper configuration models
- [ ] Add unit tests

### 4.5 Scraping Worker - Infrastructure (ScrapingWorker.Infrastructure)
- [ ] Migrate `BaseScraper` from Infrastructure
- [ ] Migrate all site-specific scrapers:
  - [ ] `Auto123Scraper`
  - [ ] `AutotraderScraper`
  - [ ] `CarFaxScraper`
  - [ ] `CarGurusScraper`
  - [ ] `CarMaxScraper`
  - [ ] `CarvanaScraper`
  - [ ] `ClutchScraper`
  - [ ] `KijijiScraper`
  - [ ] `TabangiMotorsScraper`
  - [ ] `TrueCarScraper`
  - [ ] `VroomScraper`
- [ ] Migrate `ScraperFactory` from Infrastructure
- [ ] Migrate `RateLimitingScraperDecorator` from Infrastructure
- [ ] Migrate `HealthMonitoringScraperDecorator` from Infrastructure
- [ ] Migrate `IRateLimiter` and `RateLimiter` from Infrastructure
- [ ] Migrate `IUserAgentService` and `UserAgentService` from Infrastructure
- [ ] Migrate `IBrowserContextFactory` from Infrastructure
- [ ] Migrate `IResourceManager` and `ResourceManager` from Infrastructure
- [ ] Add Microsoft.Playwright dependency
- [ ] Create Redis-based response caching
- [ ] Add integration tests

### 4.6 Scraping Worker - Service (ScrapingWorker.Service)
- [ ] Implement background worker to consume scraping jobs
- [ ] Migrate `ConcurrentScrapingEngine` from Infrastructure
- [ ] Subscribe to `ScrapingJobCreatedEvent`
- [ ] Publish `ListingScrapedEvent` for each scraped listing
- [ ] Implement health monitoring
- [ ] Add worker tests

### 4.7 Scraper Health Service
- [ ] Migrate `ScraperHealthRecord` aggregate from Core
- [ ] Migrate `IScraperHealthService` from Core
- [ ] Migrate `ScraperHealthService` from Infrastructure
- [ ] Add health status endpoint

---

## Phase 5: Listing Service

### 5.1 Domain Layer (Listing.Core)
- [ ] Migrate `Listing` aggregate from Core (remove TenantId)
- [ ] Migrate `PriceHistory` aggregate from Core (remove TenantId)
- [ ] Migrate listing-related enums from Core
- [ ] Create domain events:
  - [ ] `ListingCreatedEvent`
  - [ ] `ListingUpdatedEvent`
  - [ ] `ListingDeletedEvent`
  - [ ] `PriceChangedEvent`
- [ ] Create listing validation rules
- [ ] Add unit tests

### 5.2 Infrastructure Layer (Listing.Infrastructure)
- [ ] Create `ListingDbContext` with Listing, PriceHistory entities
- [ ] Migrate entity configurations from Infrastructure
- [ ] Create EF Core migrations
- [ ] Implement price history tracking
- [ ] Create Elasticsearch index for listings (optional)
- [ ] Add integration tests

### 5.3 API Layer (Listing.Api)
- [ ] Implement endpoints:
  - [ ] `GET /listings`
  - [ ] `GET /listings/{id}`
  - [ ] `POST /listings`
  - [ ] `PUT /listings/{id}`
  - [ ] `DELETE /listings/{id}`
  - [ ] `GET /listings/{id}/price-history`
  - [ ] `POST /listings/batch`
- [ ] Add filtering, sorting, pagination
- [ ] Add Swagger documentation
- [ ] Add API tests

### 5.4 Event Handling
- [ ] Subscribe to `ListingScrapedEvent` from Scraping Worker
- [ ] Publish `ListingCreatedEvent` after creation
- [ ] Publish `PriceChangedEvent` when price changes
- [ ] Publish `ListingUpdatedEvent` on updates

---

## Phase 6: Deduplication Service

### 6.1 Domain Layer (Deduplication.Core)
- [ ] Migrate `DeduplicationConfig` aggregate from Core (remove TenantId)
- [ ] Migrate `AuditEntry` aggregate from Core (remove TenantId)
- [ ] Migrate `ReviewItem` aggregate from Core (remove TenantId)
- [ ] Migrate `IDuplicateDetectionService` interface from Core
- [ ] Migrate `IFuzzyMatchingService` interface from Core
- [ ] Migrate fuzzy matching calculator interfaces from Core
- [ ] Migrate `IReviewService` interface from Core
- [ ] Migrate `IDeduplicationConfigService` interface from Core
- [ ] Migrate `IDeduplicationAuditService` interface from Core
- [ ] Migrate `IAccuracyMetricsService` interface from Core
- [ ] Create domain events:
  - [ ] `DeduplicationCompletedEvent`
  - [ ] `DuplicateFoundEvent`
  - [ ] `ReviewRequiredEvent`
- [ ] Add unit tests

### 6.2 Infrastructure Layer (Deduplication.Infrastructure)
- [ ] Create `DeduplicationDbContext`
- [ ] Migrate entity configurations
- [ ] Create EF Core migrations
- [ ] Migrate `DuplicateDetectionService` from Core
- [ ] Migrate `FuzzyMatchingService` from Core
- [ ] Migrate matching calculators from Core:
  - [ ] `LevenshteinCalculator`
  - [ ] `NumericProximityCalculator`
  - [ ] `ConfidenceScoreCalculator`
  - [ ] `GeoDistanceCalculator`
- [ ] Migrate `ReviewService` from Core
- [ ] Migrate `DeduplicationConfigService` from Core
- [ ] Migrate `DeduplicationAuditService` from Core
- [ ] Migrate `AccuracyMetricsService` from Core
- [ ] Create HTTP client for Image Service (hash comparison)
- [ ] Create HTTP client for Listing Service (listing data)
- [ ] Add Redis caching for matching results
- [ ] Add integration tests

### 6.3 API Layer (Deduplication.Api)
- [ ] Implement endpoints:
  - [ ] `POST /deduplication/analyze`
  - [ ] `GET /deduplication/config`
  - [ ] `PUT /deduplication/config`
  - [ ] `GET /deduplication/audit`
  - [ ] `GET /deduplication/review-queue`
  - [ ] `POST /deduplication/review/{id}/resolve`
  - [ ] `GET /deduplication/metrics`
- [ ] Add Swagger documentation
- [ ] Add API tests

### 6.4 Event Handling
- [ ] Subscribe to `ListingCreatedEvent` to trigger deduplication
- [ ] Subscribe to `ImageHashCalculatedEvent` for image-based matching
- [ ] Publish `DeduplicationCompletedEvent` after analysis
- [ ] Publish `DuplicateFoundEvent` when duplicate detected
- [ ] Publish `ReviewRequiredEvent` for near-matches

---

## Phase 7: Vehicle Aggregation Service

### 7.1 Domain Layer (VehicleAggregation.Core)
- [ ] Migrate `Vehicle` aggregate from Core (remove TenantId)
- [ ] Create vehicle matching rules
- [ ] Create domain events:
  - [ ] `VehicleCreatedEvent`
  - [ ] `VehicleUpdatedEvent`
  - [ ] `BestPriceChangedEvent`
- [ ] Add unit tests

### 7.2 Infrastructure Layer (VehicleAggregation.Infrastructure)
- [ ] Create `VehicleDbContext`
- [ ] Migrate entity configurations
- [ ] Create EF Core migrations
- [ ] Create HTTP client for Listing Service
- [ ] Implement best price calculation logic
- [ ] Add integration tests

### 7.3 API Layer (VehicleAggregation.Api)
- [ ] Implement endpoints:
  - [ ] `GET /vehicles`
  - [ ] `GET /vehicles/{id}`
  - [ ] `GET /vehicles/{id}/listings`
  - [ ] `GET /vehicles/{id}/price-comparison`
- [ ] Add Swagger documentation
- [ ] Add API tests

### 7.4 Event Handling
- [ ] Subscribe to `DeduplicationCompletedEvent`
- [ ] Subscribe to `DuplicateFoundEvent`
- [ ] Subscribe to `ListingUpdatedEvent`
- [ ] Publish `VehicleCreatedEvent` when new vehicle created
- [ ] Publish `BestPriceChangedEvent` when best price changes

---

## Phase 8: Search Service

### 8.1 Domain Layer (Search.Core)
- [ ] Migrate `SearchProfile` aggregate from Core (remove TenantId)
- [ ] Migrate `ISearchService` interface from Core
- [ ] Migrate `ISearchProfileService` interface from Core
- [ ] Migrate `IAutoCompleteService` interface from Core
- [ ] Create search criteria models
- [ ] Add unit tests

### 8.2 Infrastructure Layer (Search.Infrastructure)
- [ ] Create `SearchDbContext` for profiles
- [ ] Migrate entity configurations
- [ ] Create EF Core migrations
- [ ] Migrate `SearchService` from Core
- [ ] Migrate `SearchProfileService` from Core
- [ ] Migrate `AutoCompleteService` from Core
- [ ] Set up Elasticsearch client
- [ ] Create Elasticsearch index mapping for listings
- [ ] Create HTTP client for Listing Service
- [ ] Add integration tests

### 8.3 API Layer (Search.Api)
- [ ] Implement endpoints:
  - [ ] `POST /search`
  - [ ] `GET /search/autocomplete`
  - [ ] `GET /search/profiles`
  - [ ] `POST /search/profiles`
  - [ ] `PUT /search/profiles/{id}`
  - [ ] `DELETE /search/profiles/{id}`
  - [ ] `GET /search/facets`
- [ ] Add Swagger documentation
- [ ] Add API tests

### 8.4 Event Handling
- [ ] Subscribe to `ListingCreatedEvent` to index new listings
- [ ] Subscribe to `ListingUpdatedEvent` to update index
- [ ] Subscribe to `ListingDeletedEvent` to remove from index

---

## Phase 9: Dealer Service

### 9.1 Domain Layer (Dealer.Core)
- [ ] Migrate `Dealer` aggregate from Core (remove TenantId)
- [ ] Migrate `IRelistedDetectionService` interface from Core
- [ ] Create dealer normalization rules
- [ ] Create reliability scoring logic
- [ ] Create domain events:
  - [ ] `DealerCreatedEvent`
  - [ ] `DealerUpdatedEvent`
  - [ ] `ReliabilityScoreChangedEvent`
- [ ] Add unit tests

### 9.2 Infrastructure Layer (Dealer.Infrastructure)
- [ ] Create `DealerDbContext`
- [ ] Migrate entity configurations
- [ ] Create EF Core migrations
- [ ] Migrate `RelistedDetectionService` from Core
- [ ] Create HTTP client for Listing Service
- [ ] Implement dealer deduplication logic
- [ ] Add integration tests

### 9.3 API Layer (Dealer.Api)
- [ ] Implement endpoints:
  - [ ] `GET /dealers`
  - [ ] `GET /dealers/{id}`
  - [ ] `GET /dealers/{id}/inventory`
  - [ ] `GET /dealers/{id}/reliability`
  - [ ] `GET /dealers/{id}/relisted`
- [ ] Add Swagger documentation
- [ ] Add API tests

### 9.4 Event Handling
- [ ] Subscribe to `ListingCreatedEvent` to extract dealer info
- [ ] Publish `DealerCreatedEvent` for new dealers
- [ ] Publish `ReliabilityScoreChangedEvent` when score updates

---

## Phase 10: Alert Service

### 10.1 Domain Layer (Alert.Core)
- [ ] Migrate `Alert` aggregate from Core (remove TenantId)
- [ ] Migrate `AlertNotification` aggregate from Core (remove TenantId)
- [ ] Migrate `AlertCriteria` from Core
- [ ] Migrate `NotificationMethod` enum from Core
- [ ] Migrate `IAlertService` interface from Core
- [ ] Create alert matching logic
- [ ] Create domain events:
  - [ ] `AlertCreatedEvent`
  - [ ] `AlertTriggeredEvent`
- [ ] Add unit tests

### 10.2 Infrastructure Layer (Alert.Infrastructure)
- [ ] Create `AlertDbContext`
- [ ] Migrate entity configurations
- [ ] Create EF Core migrations
- [ ] Migrate `AlertService` from Core
- [ ] Create HTTP client for Listing Service
- [ ] Implement background alert matching worker
- [ ] Add integration tests

### 10.3 API Layer (Alert.Api)
- [ ] Implement endpoints:
  - [ ] `GET /alerts`
  - [ ] `GET /alerts/{id}`
  - [ ] `POST /alerts`
  - [ ] `PUT /alerts/{id}`
  - [ ] `DELETE /alerts/{id}`
  - [ ] `GET /alerts/{id}/history`
- [ ] Add Swagger documentation
- [ ] Add API tests

### 10.4 Event Handling
- [ ] Subscribe to `ListingCreatedEvent` to check against alerts
- [ ] Subscribe to `PriceChangedEvent` to check price alerts
- [ ] Publish `AlertTriggeredEvent` when alert matches

---

## Phase 11: WatchList Service

### 11.1 Domain Layer (WatchList.Core)
- [ ] Migrate `WatchedListing` aggregate from Core (remove TenantId)
- [ ] Migrate `IWatchListService` interface from Core
- [ ] Create domain events:
  - [ ] `ListingWatchedEvent`
  - [ ] `ListingUnwatchedEvent`
- [ ] Add unit tests

### 11.2 Infrastructure Layer (WatchList.Infrastructure)
- [ ] Create `WatchListDbContext`
- [ ] Migrate entity configurations
- [ ] Create EF Core migrations
- [ ] Migrate `WatchListService` from Core
- [ ] Create HTTP client for Listing Service
- [ ] Add integration tests

### 11.3 API Layer (WatchList.Api)
- [ ] Implement endpoints:
  - [ ] `GET /watchlist`
  - [ ] `POST /watchlist`
  - [ ] `DELETE /watchlist/{listingId}`
  - [ ] `GET /watchlist/changes`
- [ ] Add Swagger documentation
- [ ] Add API tests

### 11.4 Event Handling
- [ ] Subscribe to `ListingUpdatedEvent` to track changes
- [ ] Subscribe to `PriceChangedEvent` to notify watchers
- [ ] Subscribe to `ListingDeletedEvent` to handle removed listings

---

## Phase 12: Notification Service

### 12.1 Domain Layer (Notification.Core)
- [ ] Create `NotificationTemplate` entity
- [ ] Create `NotificationLog` entity
- [ ] Create `Webhook` entity
- [ ] Create domain events:
  - [ ] `NotificationSentEvent`
  - [ ] `NotificationFailedEvent`
- [ ] Add unit tests

### 12.2 Infrastructure Layer (Notification.Infrastructure)
- [ ] Create `NotificationDbContext`
- [ ] Create entity configurations
- [ ] Create EF Core migrations
- [ ] Implement email sender (SendGrid/AWS SES)
- [ ] Implement webhook dispatcher
- [ ] Implement notification template engine
- [ ] Add integration tests

### 12.3 API Layer (Notification.Api)
- [ ] Implement endpoints:
  - [ ] `POST /notifications/send`
  - [ ] `GET /notifications/templates`
  - [ ] `POST /notifications/templates`
  - [ ] `POST /notifications/webhooks`
  - [ ] `GET /notifications/webhooks`
  - [ ] `DELETE /notifications/webhooks/{id}`
  - [ ] `GET /notifications/history`
- [ ] Add Swagger documentation
- [ ] Add API tests

### 12.4 Event Handling
- [ ] Subscribe to `AlertTriggeredEvent` to send notifications
- [ ] Subscribe to `ReportGeneratedEvent` to send report links
- [ ] Publish `NotificationSentEvent` after successful delivery

---

## Phase 13: Reporting Service

### 13.1 Domain Layer (Reporting.Core)
- [ ] Migrate `Report` aggregate from Core (remove TenantId)
- [ ] Migrate `ScheduledReport` aggregate from Core (remove TenantId)
- [ ] Migrate `ReportFormat` enum from Core
- [ ] Migrate `ReportStatus` enum from Core
- [ ] Migrate `IReportGenerationService` interface from Core
- [ ] Migrate `IReportGenerator` interface from Infrastructure
- [ ] Migrate `IScheduledReportService` interface from Core
- [ ] Migrate `IStatisticsService` interface from Core
- [ ] Create domain events:
  - [ ] `ReportGeneratedEvent`
  - [ ] `ReportFailedEvent`
- [ ] Add unit tests

### 13.2 Infrastructure Layer (Reporting.Infrastructure)
- [ ] Create `ReportingDbContext`
- [ ] Migrate entity configurations
- [ ] Create EF Core migrations
- [ ] Migrate `ReportGenerationService` from Core
- [ ] Migrate `ScheduledReportService` from Core
- [ ] Migrate `ReportSchedulerService` from Core
- [ ] Migrate `StatisticsService` from Core
- [ ] Migrate report generators from Infrastructure:
  - [ ] `ExcelReportGenerator` (ClosedXML)
  - [ ] `HtmlReportGenerator` (Scriban)
  - [ ] `PdfReportGenerator` (PDFsharp)
- [ ] Configure blob storage for generated reports
- [ ] Create HTTP client for Listing Service
- [ ] Create HTTP client for Vehicle Service
- [ ] Add ClosedXML, PDFsharp, Scriban dependencies
- [ ] Add integration tests

### 13.3 API Layer (Reporting.Api)
- [ ] Implement endpoints:
  - [ ] `POST /reports/generate`
  - [ ] `GET /reports/{id}`
  - [ ] `GET /reports/{id}/download`
  - [ ] `GET /reports/templates`
  - [ ] `POST /reports/schedule`
  - [ ] `GET /reports/scheduled`
  - [ ] `PUT /reports/scheduled/{id}`
  - [ ] `DELETE /reports/scheduled/{id}`
- [ ] Add Swagger documentation
- [ ] Add API tests

### 13.4 Event Publishing
- [ ] Publish `ReportGeneratedEvent` after generation
- [ ] Publish `ReportFailedEvent` on failure

---

## Phase 14: Dashboard Service

### 14.1 Domain Layer (Dashboard.Core)
- [ ] Migrate `IDashboardService` interface from Core
- [ ] Create dashboard aggregation models
- [ ] Define dashboard widget types
- [ ] Add unit tests

### 14.2 Infrastructure Layer (Dashboard.Infrastructure)
- [ ] Migrate `DashboardService` from Core
- [ ] Create HTTP clients for all dependent services:
  - [ ] Listing Service
  - [ ] Vehicle Service
  - [ ] Alert Service
  - [ ] WatchList Service
  - [ ] Scraping Orchestration Service
- [ ] Create Redis caching for dashboard data
- [ ] Implement SignalR hub for real-time updates
- [ ] Add integration tests

### 14.3 API Layer (Dashboard.Api)
- [ ] Implement endpoints:
  - [ ] `GET /dashboard/overview`
  - [ ] `GET /dashboard/market-trends`
  - [ ] `GET /dashboard/scraping-status`
  - [ ] `GET /dashboard/health`
  - [ ] `WS /dashboard/realtime` (SignalR)
- [ ] Add SignalR configuration
- [ ] Add Swagger documentation
- [ ] Add API tests

### 14.4 Event Handling
- [ ] Subscribe to `ListingCreatedEvent` for real-time updates
- [ ] Subscribe to `ScrapingJobCompletedEvent` for scraping status
- [ ] Subscribe to `PriceChangedEvent` for market trends

---

## Phase 15: Geographic Service

### 15.1 Domain Layer (Geographic.Core)
- [ ] Migrate `CustomMarket` aggregate from Core (remove TenantId)
- [ ] Migrate `ICustomMarketService` interface from Core
- [ ] Migrate `IGeoDistanceCalculator` interface from Core
- [ ] Create postal code lookup models
- [ ] Add unit tests

### 15.2 Infrastructure Layer (Geographic.Infrastructure)
- [ ] Create `GeographicDbContext` with PostGIS support
- [ ] Migrate entity configurations
- [ ] Create EF Core migrations
- [ ] Migrate `CustomMarketService` from Core
- [ ] Migrate `GeoDistanceCalculator` from Core
- [ ] Import Canadian postal code database
- [ ] Add integration tests

### 15.3 API Layer (Geographic.Api)
- [ ] Implement endpoints:
  - [ ] `GET /markets`
  - [ ] `POST /markets`
  - [ ] `PUT /markets/{id}`
  - [ ] `DELETE /markets/{id}`
  - [ ] `POST /markets/geocode`
  - [ ] `POST /markets/distance`
  - [ ] `POST /markets/within`
- [ ] Add Swagger documentation
- [ ] Add API tests

---

## Phase 16: Configuration Service

### 16.1 Domain Layer (Configuration.Core)
- [ ] Create `ServiceConfiguration` entity
- [ ] Create `FeatureFlag` entity
- [ ] Migrate `ResourceThrottle` aggregate from Core (remove TenantId)
- [ ] Migrate `IResourceThrottleService` interface from Core
- [ ] Add unit tests

### 16.2 Infrastructure Layer (Configuration.Infrastructure)
- [ ] Create `ConfigurationDbContext`
- [ ] Create entity configurations
- [ ] Create EF Core migrations
- [ ] Migrate `ResourceThrottleService` from Core
- [ ] Integrate with Consul/etcd (optional)
- [ ] Implement configuration versioning
- [ ] Add integration tests

### 16.3 API Layer (Configuration.Api)
- [ ] Implement endpoints:
  - [ ] `GET /config/{service}`
  - [ ] `PUT /config/{service}`
  - [ ] `GET /config/features`
  - [ ] `PUT /config/features/{flag}`
  - [ ] `GET /config/throttle`
  - [ ] `PUT /config/throttle`
- [ ] Add Swagger documentation
- [ ] Add API tests

---

## Phase 17: Backup Service

### 17.1 Domain Layer (Backup.Core)
- [ ] Create `BackupJob` entity
- [ ] Create `ImportJob` entity
- [ ] Migrate `IBackupService` interface from Core
- [ ] Migrate `IDataImportService` interface from Core
- [ ] Create domain events:
  - [ ] `BackupCompletedEvent`
  - [ ] `RestoreCompletedEvent`
  - [ ] `ImportCompletedEvent`
- [ ] Add unit tests

### 17.2 Infrastructure Layer (Backup.Infrastructure)
- [ ] Create `BackupDbContext`
- [ ] Create entity configurations
- [ ] Create EF Core migrations
- [ ] Migrate `BackupService` from Infrastructure
- [ ] Migrate `DataImportService` from Infrastructure
- [ ] Configure blob storage for backups
- [ ] Create HTTP clients for all data services
- [ ] Add CsvHelper, ClosedXML dependencies
- [ ] Add integration tests

### 17.3 Worker Service (Backup.Service)
- [ ] Implement backup worker
- [ ] Implement restore worker
- [ ] Implement import worker
- [ ] Schedule automatic backups
- [ ] Add worker tests

### 17.4 API Endpoints (add to Configuration.Api or separate)
- [ ] `POST /backup`
- [ ] `GET /backups`
- [ ] `POST /restore/{backupId}`
- [ ] `POST /import`
- [ ] `GET /import/{jobId}`

---

## Phase 18: API Gateway

### 18.1 Gateway Configuration
- [ ] Install YARP or Ocelot
- [ ] Configure routing to all microservices:
  - [ ] `/api/auth/*` → Identity Service
  - [ ] `/api/listings/*` → Listing Service
  - [ ] `/api/vehicles/*` → Vehicle Aggregation Service
  - [ ] `/api/search/*` → Search Service
  - [ ] `/api/alerts/*` → Alert Service
  - [ ] `/api/watchlist/*` → WatchList Service
  - [ ] `/api/reports/*` → Reporting Service
  - [ ] `/api/dashboard/*` → Dashboard Service
  - [ ] `/api/dealers/*` → Dealer Service
  - [ ] `/api/images/*` → Image Service
  - [ ] `/api/scraping/*` → Scraping Orchestration Service
  - [ ] `/api/markets/*` → Geographic Service
  - [ ] `/api/notifications/*` → Notification Service
  - [ ] `/api/config/*` → Configuration Service
  - [ ] `/api/deduplication/*` → Deduplication Service
- [ ] Configure JWT authentication validation
- [ ] Configure rate limiting
- [ ] Configure request/response logging
- [ ] Configure SSL termination
- [ ] Add health check aggregation
- [ ] Add Swagger aggregation (optional)
- [ ] Add integration tests

### 18.2 Resilience
- [ ] Configure circuit breakers
- [ ] Configure retry policies
- [ ] Configure timeout policies
- [ ] Configure load balancing

---

## Phase 19: Cleanup & Deletion

### 19.1 Verify All Functionality Migrated
- [ ] Run full integration test suite across all microservices
- [ ] Verify all API endpoints work through API Gateway
- [ ] Verify all event flows work end-to-end
- [ ] Verify all data migrations completed successfully
- [ ] Perform user acceptance testing

### 19.2 Remove Monolithic Projects from Solution
- [ ] Remove `AutomatedMarketIntelligenceTool.Core` from solution
- [ ] Remove `AutomatedMarketIntelligenceTool.Infrastructure` from solution
- [ ] Remove `AutomatedMarketIntelligenceTool.Application` from solution
- [ ] Remove `AutomatedMarketIntelligenceTool.Api` from solution
- [ ] Remove `AutomatedMarketIntelligenceTool.Cli` from solution (or migrate to gateway client)

### 19.3 Remove Monolithic Test Projects
- [ ] Remove `AutomatedMarketIntelligenceTool.Core.Tests` from solution
- [ ] Remove `AutomatedMarketIntelligenceTool.Infrastructure.Tests` from solution
- [ ] Remove `AutomatedMarketIntelligenceTool.Application.Tests` from solution
- [ ] Remove `AutomatedMarketIntelligenceTool.Api.Tests` from solution
- [ ] Remove `AutomatedMarketIntelligenceTool.Cli.Tests` from solution

### 19.4 Delete Monolithic Source Code
- [ ] Delete `src/AutomatedMarketIntelligenceTool.Core/` directory
- [ ] Delete `src/AutomatedMarketIntelligenceTool.Infrastructure/` directory
- [ ] Delete `src/AutomatedMarketIntelligenceTool.Application/` directory
- [ ] Delete `src/AutomatedMarketIntelligenceTool.Api/` directory
- [ ] Delete `src/AutomatedMarketIntelligenceTool.Cli/` directory

### 19.5 Delete Monolithic Test Code
- [ ] Delete `tests/AutomatedMarketIntelligenceTool.Core.Tests/` directory
- [ ] Delete `tests/AutomatedMarketIntelligenceTool.Infrastructure.Tests/` directory
- [ ] Delete `tests/AutomatedMarketIntelligenceTool.Application.Tests/` directory
- [ ] Delete `tests/AutomatedMarketIntelligenceTool.Api.Tests/` directory
- [ ] Delete `tests/AutomatedMarketIntelligenceTool.Cli.Tests/` directory

### 19.6 Delete Playground Projects (optional)
- [ ] Delete `playground/VolkswagonJettaSearch/` directory
- [ ] Delete `playground/Tabangimotors/` directory
- [ ] Remove playground solution folder

### 19.7 Update Solution File
- [ ] Remove all deleted project references from `.sln` file
- [ ] Reorganize solution folders for microservices
- [ ] Verify solution builds successfully

---

## Phase 20: Final Validation

### 20.1 Performance Testing
- [ ] Run load tests against API Gateway
- [ ] Verify service-to-service latency is acceptable
- [ ] Verify message broker throughput
- [ ] Verify database performance per service

### 20.2 Security Audit
- [ ] Verify JWT authentication works across services
- [ ] Verify service-to-service authentication (mTLS)
- [ ] Verify rate limiting is effective
- [ ] Verify no sensitive data exposed in logs

### 20.3 Monitoring & Observability
- [ ] Verify all services report health correctly
- [ ] Verify distributed tracing works end-to-end
- [ ] Verify centralized logging captures all events
- [ ] Verify metrics dashboards show correct data

### 20.4 Documentation
- [ ] Update API documentation
- [ ] Update deployment documentation
- [ ] Update developer onboarding guide
- [ ] Archive monolithic codebase documentation

### 20.5 Production Deployment
- [ ] Deploy to staging environment
- [ ] Run smoke tests
- [ ] Deploy to production
- [ ] Monitor for issues
- [ ] Celebrate completion! 🎉

---

## Migration Dependency Graph

```
Phase 0: Pre-Migration Preparation
    │
    ▼
Phase 1: Shared Infrastructure
    │
    ├──────────────────────────────────┐
    ▼                                  ▼
Phase 2: Identity Service      Phase 3: Image Service
    │                                  │
    │    ┌─────────────────────────────┤
    │    │                             │
    ▼    ▼                             │
Phase 4: Scraping Services             │
    │                                  │
    ▼                                  │
Phase 5: Listing Service ◄─────────────┘
    │
    ├──────────────────┬───────────────┬───────────────┐
    ▼                  ▼               ▼               ▼
Phase 6:           Phase 8:        Phase 9:       Phase 10:
Deduplication      Search          Dealer         Alert
    │                                              │
    ▼                                              ▼
Phase 7:                                       Phase 11:
Vehicle Aggregation                            WatchList
    │                                              │
    │              ┌───────────────────────────────┘
    │              │
    ▼              ▼
Phase 13: Reporting Service
    │
    ▼
Phase 14: Dashboard Service
    │
    ├──────────────────┬───────────────┐
    ▼                  ▼               ▼
Phase 15:          Phase 16:       Phase 17:
Geographic         Configuration   Backup
                       │
                       ▼
               Phase 12: Notification
                       │
                       ▼
               Phase 18: API Gateway
                       │
                       ▼
               Phase 19: Cleanup & Deletion
                       │
                       ▼
               Phase 20: Final Validation
```

---

## Estimated Effort Summary

| Phase | Description | Complexity |
|-------|-------------|------------|
| 0 | Pre-Migration Preparation | Medium |
| 1 | Shared Infrastructure | Medium |
| 2 | Identity Service | Medium |
| 3 | Image Service | Low |
| 4 | Scraping Services | High |
| 5 | Listing Service | High |
| 6 | Deduplication Service | High |
| 7 | Vehicle Aggregation Service | Medium |
| 8 | Search Service | Medium |
| 9 | Dealer Service | Medium |
| 10 | Alert Service | Medium |
| 11 | WatchList Service | Low |
| 12 | Notification Service | Medium |
| 13 | Reporting Service | High |
| 14 | Dashboard Service | Medium |
| 15 | Geographic Service | Low |
| 16 | Configuration Service | Low |
| 17 | Backup Service | Medium |
| 18 | API Gateway | Medium |
| 19 | Cleanup & Deletion | Low |
| 20 | Final Validation | Medium |

---

## Risk Mitigation

1. **Data Migration Risk**: Run parallel systems during transition
2. **Service Communication Failures**: Implement circuit breakers and fallbacks
3. **Performance Degradation**: Monitor latency and optimize hot paths
4. **Team Learning Curve**: Provide training on microservices patterns
5. **Rollback Strategy**: Keep monolithic system running until full validation complete
