# Phase 2 Technical Implementation Roadmap

## Automated Market Intelligence Tool - Enhanced Search & Multi-Site Support

**Version:** 1.0
**Last Updated:** 2026-01-08
**Status:** Planning
**Prerequisites:** Phase 1 Complete

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Phase 2 Objectives](#phase-2-objectives)
3. [Implementation Philosophy](#implementation-philosophy)
4. [Architecture Evolution](#architecture-evolution)
5. [Implementation Sequence](#implementation-sequence)
6. [Sprint Breakdown](#sprint-breakdown)
7. [Technical Specifications](#technical-specifications)
8. [Risk Mitigation](#risk-mitigation)
9. [Definition of Done](#definition-of-done)

---

## Executive Summary

Phase 2 builds upon the MVP foundation to deliver:
- Enhanced scraping with rate limiting and site selection
- Extended search filters (condition, transmission, fuel type, body style)
- SQL Server support for enterprise deployments
- Comprehensive CLI with list, export, and config subcommands
- New listing highlighting and price change tracking
- Improved error handling and progress indication

### Key Metrics

| Metric | Phase 1 | Phase 2 Target |
|--------|---------|----------------|
| Sites Supported | 2-3 | 4-5 |
| Search Filters | 6 | 12 |
| Output Formats | 3 | 3 (enhanced) |
| Database Providers | SQLite | SQLite + SQL Server |
| CLI Commands | 2 | 5 |

### Build Order Summary

```
1. Infrastructure (Database & Rate Limiting)
2. Extended Data Models (New Enums, Entities)
3. Enhanced Scrapers (Site Selection, Error Handling)
4. Search Filter Extensions (Condition, Transmission, FuelType, BodyStyle)
5. CLI Enhancements (New Commands, Progress, Help)
6. Reporting Enhancements (Highlighting, Sorting, Price Tracking)
```

---

## Phase 2 Objectives

### Primary Goals

| ID | Objective | Specs Coverage |
|----|-----------|----------------|
| P2-OBJ-01 | Site selection and exclusion | REQ-WS-001 (AC-001.4-001.6) |
| P2-OBJ-02 | Rate limiting and throttling | REQ-WS-003 (AC-003.1-003.5) |
| P2-OBJ-03 | Extended data extraction | REQ-WS-004 (AC-004.3, AC-004.5) |
| P2-OBJ-04 | Advanced search filters | REQ-SC-006 through REQ-SC-009 |
| P2-OBJ-05 | SQL Server support | REQ-DP-001 (AC-001.2-001.4) |
| P2-OBJ-06 | CLI list, export, config commands | REQ-CLI-001 (AC-001.3-001.5) |
| P2-OBJ-07 | New listing identification | REQ-DD-007 (AC-007.1-007.5) |
| P2-OBJ-08 | Price change detection | REQ-RP-004 (AC-004.1-004.3) |

### Feature Requirements Matrix

| Feature | Requirements | Priority |
|---------|--------------|----------|
| Site Selection | AC-001.4, AC-001.5, AC-001.6 | P0 |
| Rate Limiting | AC-003.1, AC-003.2, AC-003.3, AC-003.5 | P0 |
| Condition Filter | AC-006.1, AC-006.2, AC-006.3, AC-006.4 | P0 |
| Transmission Filter | AC-007.1, AC-007.2, AC-007.3, AC-007.4 | P0 |
| Fuel Type Filter | AC-008.1, AC-008.2, AC-008.3, AC-008.4 | P0 |
| Body Style Filter | AC-009.1, AC-009.2, AC-009.3, AC-009.4 | P0 |
| SQL Server Support | AC-001.2, AC-001.4, AC-001.6 | P1 |
| City/State Location | AC-002.1, AC-002.2, AC-002.4, AC-002.5 | P1 |
| List Command | AC-001.3 | P0 |
| Export Command | AC-001.4 | P0 |
| Config Command | AC-001.5 | P1 |
| Progress Bars | AC-004.1, AC-004.2, AC-004.4 | P0 |
| New Listing Highlighting | AC-003.1, AC-003.2, AC-003.3, AC-003.4 | P0 |
| Price Change Notifications | AC-004.1, AC-004.2, AC-004.3 | P0 |
| Sorting Options | AC-005.1-AC-005.7 | P0 |
| Search Session Logging | AC-007.1, AC-007.2, AC-007.3, AC-007.4 | P1 |

---

## Implementation Philosophy

### Build Order Rationale

**Why Infrastructure First?**

```
┌──────────────────────────────────────────────────────────────────┐
│  1. INFRASTRUCTURE LAYER                                          │
│     ├── SQL Server Provider (enterprise deployment foundation)    │
│     ├── Rate Limiting Service (ethical scraping requirement)      │
│     └── Search Session Entity (tracking foundation)               │
│                                                                    │
│  WHY FIRST: All subsequent features depend on these capabilities │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  2. DATA MODEL EXTENSIONS                                         │
│     ├── New Enums (BodyStyle, Drivetrain, SellerType)            │
│     ├── PriceHistory Entity (price tracking)                      │
│     └── SearchSession Entity (audit trail)                        │
│                                                                    │
│  WHY SECOND: Extended scrapers need target models defined first  │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  3. SCRAPER ENHANCEMENTS                                          │
│     ├── Site Selection/Exclusion Logic                            │
│     ├── Rate Limiting Integration                                 │
│     ├── Extended Field Extraction                                 │
│     └── CarGurus Scraper (new site)                              │
│                                                                    │
│  WHY THIRD: Scrapers populate extended models                    │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  4. SEARCH SERVICE EXTENSIONS                                     │
│     ├── Condition Filter Implementation                           │
│     ├── Transmission Filter Implementation                        │
│     ├── Fuel Type Filter Implementation                           │
│     ├── Body Style Filter Implementation                          │
│     └── City/State Location Filtering                             │
│                                                                    │
│  WHY FOURTH: Filters require data to exist in extended fields    │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  5. CLI ENHANCEMENTS                                              │
│     ├── List Command (database queries)                           │
│     ├── Export Command (file output)                              │
│     ├── Config Command (settings management)                      │
│     ├── Help System Enhancement                                   │
│     └── Progress Indication                                       │
│                                                                    │
│  WHY FIFTH: CLI orchestrates all previous components             │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  6. REPORTING ENHANCEMENTS                                        │
│     ├── New Listing Highlighting                                  │
│     ├── Price Change Notifications                                │
│     ├── Sorting Implementation                                    │
│     └── Post-Search Filtering                                     │
│                                                                    │
│  WHY LAST: Requires data + filters + CLI to be functional        │
└──────────────────────────────────────────────────────────────────┘
```

### Design Patterns Applied

| Pattern | Application | Rationale |
|---------|-------------|-----------|
| **Decorator Pattern** | Rate Limiting | Wraps scrapers without modifying core logic |
| **Strategy Pattern** | Database Providers | SQLite/SQL Server interchangeable |
| **Observer Pattern** | Price Change Detection | Notify on price delta events |
| **Builder Pattern** | Search Query Builder | Complex filter composition |
| **Command Pattern** | CLI Commands | Encapsulated, testable operations |

### Critical Constraints (from Implementation Specs)

| Constraint | Impact on Phase 2 |
|------------|-------------------|
| No Repository Pattern | Direct IAutomatedMarketIntelligenceToolContext for all new queries |
| Services in Core | New filter services go in Core/Services |
| Manual DTO Mapping | ToDto() extensions for new entities |
| Serilog Structured Logging | All new services must log appropriately |
| TenantId on Aggregates | SearchSession entity requires TenantId |

---

## Architecture Evolution

### Phase 2 Architecture Additions

```
src/
├── AutomatedMarketIntelligenceTool.Core/
│   ├── Models/
│   │   ├── ListingAggregate/
│   │   │   ├── Enums/
│   │   │   │   ├── BodyStyle.cs              [NEW]
│   │   │   │   ├── Drivetrain.cs             [NEW]
│   │   │   │   └── SellerType.cs             [NEW]
│   │   │   └── Events/
│   │   │       └── ListingDeactivated.cs     [NEW]
│   │   ├── PriceHistoryAggregate/            [NEW]
│   │   │   ├── PriceHistory.cs
│   │   │   └── PriceHistoryId.cs
│   │   └── SearchSessionAggregate/           [NEW]
│   │       ├── SearchSession.cs
│   │       └── SearchSessionId.cs
│   └── Services/
│       ├── INewListingDetectionService.cs    [NEW]
│       ├── NewListingDetectionService.cs     [NEW]
│       ├── IPriceChangeDetectionService.cs   [NEW]
│       ├── PriceChangeDetectionService.cs    [NEW]
│       ├── IListingDeactivationService.cs    [NEW]
│       └── ListingDeactivationService.cs     [NEW]
│
├── AutomatedMarketIntelligenceTool.Infrastructure/
│   ├── EntityConfigurations/
│   │   ├── PriceHistoryConfiguration.cs      [NEW]
│   │   └── SearchSessionConfiguration.cs     [NEW]
│   └── Services/
│       ├── RateLimiting/                     [NEW]
│       │   ├── IRateLimiter.cs
│       │   ├── RateLimiter.cs
│       │   └── RateLimitConfiguration.cs
│       └── Scrapers/
│           ├── CarGurusScraper.cs            [NEW]
│           └── ScraperOptions.cs             [NEW]
│
├── AutomatedMarketIntelligenceTool.Api/
│   ├── Features/
│   │   ├── Listings/
│   │   │   ├── GetListingsQuery.cs           [NEW]
│   │   │   └── ExportListingsQuery.cs        [NEW]
│   │   └── Sessions/                         [NEW]
│   │       ├── CreateSearchSessionCommand.cs
│   │       └── GetSearchSessionsQuery.cs
│   └── Extensions/
│       ├── PriceHistoryExtensions.cs         [NEW]
│       └── SearchSessionExtensions.cs        [NEW]
│
└── AutomatedMarketIntelligenceTool.Cli/
    ├── Commands/
    │   ├── ListCommand.cs                    [NEW]
    │   ├── ExportCommand.cs                  [NEW]
    │   └── ConfigCommand.cs                  [NEW]
    └── Configuration/                        [NEW]
        ├── AppSettings.cs
        └── ConfigurationManager.cs
```

### Database Schema Evolution

```sql
-- New Tables

CREATE TABLE PriceHistory (
    PriceHistoryId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    ListingId UNIQUEIDENTIFIER NOT NULL,
    Price DECIMAL(12,2) NOT NULL,
    ObservedAt DATETIME2 NOT NULL,
    PriceChange DECIMAL(12,2),           -- Calculated delta
    ChangePercentage DECIMAL(5,2),       -- Calculated percentage
    CreatedAt DATETIME2 NOT NULL,

    CONSTRAINT FK_PriceHistory_Listing
        FOREIGN KEY (ListingId) REFERENCES Listings(ListingId),
    INDEX IX_PriceHistory_Listing (ListingId),
    INDEX IX_PriceHistory_TenantId (TenantId)
);

CREATE TABLE SearchSessions (
    SearchSessionId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2,
    Status NVARCHAR(20) NOT NULL,        -- Running, Completed, Failed
    SearchParameters NVARCHAR(MAX),      -- JSON serialized
    TotalListingsFound INT NOT NULL DEFAULT 0,
    NewListingsCount INT NOT NULL DEFAULT 0,
    PriceChangesCount INT NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL,

    INDEX IX_SearchSession_TenantId (TenantId),
    INDEX IX_SearchSession_StartTime (StartTime)
);

-- Schema Modifications

ALTER TABLE Listings ADD
    Drivetrain INT NULL,
    SellerType INT NULL,
    SellerName NVARCHAR(200) NULL,
    SellerPhone NVARCHAR(20) NULL,
    ListingDate DATETIME2 NULL,
    DaysOnMarket INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    DeactivatedAt DATETIME2 NULL;

-- New Indexes for Phase 2 Queries
CREATE INDEX IX_Listing_Condition ON Listings(Condition);
CREATE INDEX IX_Listing_Transmission ON Listings(Transmission);
CREATE INDEX IX_Listing_FuelType ON Listings(FuelType);
CREATE INDEX IX_Listing_BodyStyle ON Listings(BodyStyle);
CREATE INDEX IX_Listing_IsActive ON Listings(IsActive);
CREATE INDEX IX_Listing_FirstSeenDate ON Listings(FirstSeenDate);
```

---

## Implementation Sequence

### Phase 2 Implementation Timeline

```
Sprint 1: Infrastructure Foundation (Week 1-2)
    │
    ├── 1.1 SQL Server Provider Support
    │       ├── Connection string configuration
    │       ├── Provider-agnostic migrations
    │       └── Integration tests for both providers
    │
    ├── 1.2 Rate Limiting Service
    │       ├── IRateLimiter interface
    │       ├── Domain-based rate tracking
    │       └── Configurable delays (2-5 seconds default)
    │
    └── 1.3 Data Model Extensions
            ├── BodyStyle, Drivetrain, SellerType enums
            ├── PriceHistory entity
            ├── SearchSession entity
            └── Migration for new schema

Sprint 2: Scraper Enhancements (Week 3-4)
    │
    ├── 2.1 Site Selection Logic
    │       ├── --sites option implementation
    │       ├── --exclude-sites option
    │       └── Scraper factory updates
    │
    ├── 2.2 Rate Limiting Integration
    │       ├── Decorator pattern integration
    │       ├── Exponential backoff on 429
    │       └── Per-domain tracking
    │
    ├── 2.3 Extended Data Extraction
    │       ├── Transmission extraction
    │       ├── Fuel type extraction
    │       ├── Drivetrain extraction
    │       └── Dealer info extraction
    │
    └── 2.4 CarGurus Scraper
            ├── URL builder
            ├── DOM parser
            └── Pagination handler

Sprint 3: Search & Detection Services (Week 5-6)
    │
    ├── 3.1 Extended Search Filters
    │       ├── Condition filter
    │       ├── Transmission filter
    │       ├── Fuel type filter
    │       ├── Body style filter
    │       └── City/State location filter
    │
    ├── 3.2 New Listing Detection Service
    │       ├── Detection logic
    │       ├── Flag setting
    │       └── Count tracking
    │
    ├── 3.3 Price Change Detection Service
    │       ├── Change detection
    │       ├── PriceHistory creation
    │       └── Notification events
    │
    └── 3.4 Listing Deactivation Service
            ├── Staleness detection (7 days default)
            ├── IsActive flag management
            └── Reactivation logic

Sprint 4: CLI Enhancements (Week 7-8)
    │
    ├── 4.1 List Command
    │       ├── Database query integration
    │       ├── All filter options
    │       └── Pagination support
    │
    ├── 4.2 Export Command
    │       ├── JSON export
    │       ├── CSV export
    │       └── File path handling
    │
    ├── 4.3 Config Command
    │       ├── Get/Set operations
    │       ├── Configuration file management
    │       └── Environment variable support
    │
    ├── 4.4 Help Documentation
    │       ├── Global help
    │       ├── Per-command help
    │       └── Examples
    │
    └── 4.5 Progress Indication
            ├── Scraping progress bars
            ├── Site/page/listing counts
            └── Non-TTY handling

Sprint 5: Reporting & Integration (Week 9-10)
    │
    ├── 5.1 New Listing Highlighting
    │       ├── [NEW] indicator
    │       ├── Color highlighting (green)
    │       └── --new-only filter
    │
    ├── 5.2 Price Change Notifications
    │       ├── Price delta display
    │       ├── Color coding (green down, red up)
    │       └── Percentage calculation
    │
    ├── 5.3 Sorting Implementation
    │       ├── --sort option
    │       ├── Multi-field sorting
    │       └── Ascending/descending
    │
    ├── 5.4 Post-Search Filtering
    │       ├── --filter option
    │       ├── AND logic combination
    │       └── Display-only filtering
    │
    └── 5.5 Integration & Testing
            ├── E2E test suite
            ├── Performance testing
            └── Documentation updates
```

---

## Sprint Breakdown

### Sprint 1: Infrastructure Foundation

#### Goals
- Enable SQL Server as alternative database provider
- Implement rate limiting for ethical scraping
- Extend data model for Phase 2 features

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S1-01 | Add SQL Server connection string configuration | None | P0 |
| S1-02 | Create database provider factory | S1-01 | P0 |
| S1-03 | Update DbContext for provider abstraction | S1-02 | P0 |
| S1-04 | Create IRateLimiter interface | None | P0 |
| S1-05 | Implement RateLimiter with domain tracking | S1-04 | P0 |
| S1-06 | Add RateLimitConfiguration | S1-05 | P0 |
| S1-07 | Create BodyStyle enum | None | P0 |
| S1-08 | Create Drivetrain enum | None | P0 |
| S1-09 | Create SellerType enum | None | P0 |
| S1-10 | Create PriceHistory entity | None | P0 |
| S1-11 | Create SearchSession entity | None | P0 |
| S1-12 | Create PriceHistoryConfiguration | S1-10 | P0 |
| S1-13 | Create SearchSessionConfiguration | S1-11 | P0 |
| S1-14 | Update Listing entity with new fields | S1-07, S1-08, S1-09 | P0 |
| S1-15 | Create Phase 2 migration | S1-10, S1-11, S1-14 | P0 |
| S1-16 | Add SQL Server integration tests | S1-03 | P1 |

#### Key Code Artifacts

**IRateLimiter.cs** (Infrastructure/Services/RateLimiting/)
```csharp
namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;

public interface IRateLimiter
{
    Task WaitAsync(string domain, CancellationToken cancellationToken = default);
    void ReportRateLimitHit(string domain);
    RateLimitStatus GetStatus(string domain);
}
```

**RateLimiter.cs** (Infrastructure/Services/RateLimiting/)
```csharp
namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;

public class RateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, DomainRateState> _domainStates = new();
    private readonly RateLimitConfiguration _config;
    private readonly ILogger<RateLimiter> _logger;

    public async Task WaitAsync(string domain, CancellationToken cancellationToken = default)
    {
        var state = _domainStates.GetOrAdd(domain, _ => new DomainRateState());

        var delay = state.GetRequiredDelay(_config.DefaultDelayMs);
        if (delay > TimeSpan.Zero)
        {
            _logger.LogDebug("Rate limiting: waiting {Delay}ms for {Domain}",
                delay.TotalMilliseconds, domain);
            await Task.Delay(delay, cancellationToken);
        }

        state.RecordRequest();
    }

    public void ReportRateLimitHit(string domain)
    {
        var state = _domainStates.GetOrAdd(domain, _ => new DomainRateState());
        state.IncreaseBackoff();
        _logger.LogWarning("Rate limit hit for {Domain}, increasing backoff", domain);
    }
}
```

**PriceHistory.cs** (Core/Models/PriceHistoryAggregate/)
```csharp
namespace AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;

public class PriceHistory
{
    public PriceHistoryId PriceHistoryId { get; private set; }
    public Guid TenantId { get; private set; }
    public ListingId ListingId { get; private set; }
    public decimal Price { get; private set; }
    public DateTime ObservedAt { get; private set; }
    public decimal? PriceChange { get; private set; }
    public decimal? ChangePercentage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PriceHistory() { }

    public static PriceHistory Create(
        Guid tenantId,
        ListingId listingId,
        decimal price,
        decimal? previousPrice = null)
    {
        var history = new PriceHistory
        {
            PriceHistoryId = new PriceHistoryId(Guid.NewGuid()),
            TenantId = tenantId,
            ListingId = listingId,
            Price = price,
            ObservedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        if (previousPrice.HasValue && previousPrice.Value != price)
        {
            history.PriceChange = price - previousPrice.Value;
            history.ChangePercentage = previousPrice.Value != 0
                ? Math.Round((history.PriceChange.Value / previousPrice.Value) * 100, 2)
                : null;
        }

        return history;
    }
}
```

---

### Sprint 2: Scraper Enhancements

#### Goals
- Implement site selection and exclusion
- Integrate rate limiting into scrapers
- Extract extended vehicle data
- Add CarGurus as fourth scraper

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S2-01 | Add ScraperOptions configuration class | None | P0 |
| S2-02 | Implement --sites option parsing | S2-01 | P0 |
| S2-03 | Implement --exclude-sites option parsing | S2-01 | P0 |
| S2-04 | Update ScraperFactory with site filtering | S2-02, S2-03 | P0 |
| S2-05 | Create RateLimitingScraperDecorator | Sprint 1 | P0 |
| S2-06 | Integrate rate limiter into BaseScraper | S2-05 | P0 |
| S2-07 | Implement exponential backoff on 429 | S2-06 | P0 |
| S2-08 | Extend AutotraderScraper for new fields | Sprint 1 | P0 |
| S2-09 | Extend CarsComScraper for new fields | Sprint 1 | P0 |
| S2-10 | Create CarGurusScraper | S2-04 | P0 |
| S2-11 | Add headed mode option (--headed) | None | P1 |
| S2-12 | Improve error handling (CAPTCHA, block detection) | S2-06 | P1 |
| S2-13 | Add max-results option | None | P1 |
| S2-14 | Implement progress reporting | None | P1 |
| S2-15 | Add scraper integration tests | S2-08, S2-09, S2-10 | P1 |

#### Key Code Artifacts

**ScraperOptions.cs** (Infrastructure/Services/Scrapers/)
```csharp
namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class ScraperOptions
{
    public string[]? Sites { get; set; }
    public string[]? ExcludeSites { get; set; }
    public int DelayMs { get; set; } = 3000;
    public int MaxPages { get; set; } = 50;
    public int? MaxResults { get; set; }
    public bool HeadedMode { get; set; } = false;
    public int MaxRetries { get; set; } = 3;
}
```

**Updated ScraperFactory.cs**
```csharp
namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;

public class ScraperFactory : IScraperFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<ScraperFactory> _logger;

    private static readonly Dictionary<string, Type> _scraperTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["autotrader"] = typeof(AutotraderScraper),
        ["cars.com"] = typeof(CarsComScraper),
        ["carscom"] = typeof(CarsComScraper),
        ["cargurus"] = typeof(CarGurusScraper)
    };

    public IEnumerable<ISiteScraper> CreateScrapers(ScraperOptions options)
    {
        var scrapers = new List<ISiteScraper>();
        var siteNames = options.Sites?.Length > 0
            ? options.Sites
            : _scraperTypes.Keys.Distinct().ToArray();

        var excludeSet = new HashSet<string>(
            options.ExcludeSites ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var siteName in siteNames.Where(s => !excludeSet.Contains(s)))
        {
            if (_scraperTypes.TryGetValue(siteName, out var scraperType))
            {
                var scraper = (ISiteScraper)_serviceProvider.GetRequiredService(scraperType);
                scrapers.Add(new RateLimitingScraperDecorator(scraper, _rateLimiter));
            }
            else
            {
                _logger.LogWarning("Unknown scraper site: {SiteName}", siteName);
            }
        }

        return scrapers;
    }
}
```

---

### Sprint 3: Search & Detection Services

#### Goals
- Implement extended search filters
- Create new listing detection service
- Create price change detection service
- Create listing deactivation service

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S3-01 | Implement Condition filter in SearchService | Sprint 1 | P0 |
| S3-02 | Implement Transmission filter | Sprint 1 | P0 |
| S3-03 | Implement FuelType filter | Sprint 1 | P0 |
| S3-04 | Implement BodyStyle filter | Sprint 1 | P0 |
| S3-05 | Implement City/State location filter | None | P1 |
| S3-06 | Create INewListingDetectionService | None | P0 |
| S3-07 | Implement NewListingDetectionService | S3-06 | P0 |
| S3-08 | Create IPriceChangeDetectionService | Sprint 1 | P0 |
| S3-09 | Implement PriceChangeDetectionService | S3-08 | P0 |
| S3-10 | Create IListingDeactivationService | None | P0 |
| S3-11 | Implement ListingDeactivationService | S3-10 | P0 |
| S3-12 | Integrate detection services into scrape flow | S3-07, S3-09 | P0 |
| S3-13 | Add search filter unit tests | S3-01-04 | P1 |
| S3-14 | Add detection service unit tests | S3-07, S3-09, S3-11 | P1 |

#### Key Code Artifacts

**NewListingDetectionService.cs** (Core/Services/)
```csharp
namespace AutomatedMarketIntelligenceTool.Core.Services;

public class NewListingDetectionService : INewListingDetectionService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<NewListingDetectionService> _logger;

    public async Task<NewListingResult> ProcessListingAsync(
        Listing listing,
        CancellationToken cancellationToken = default)
    {
        var isNew = listing.FirstSeenDate == listing.LastSeenDate;

        if (isNew)
        {
            _logger.LogInformation(
                "New listing detected: {ListingId} - {Year} {Make} {Model}",
                listing.ListingId, listing.Year, listing.Make, listing.Model);
        }

        return new NewListingResult
        {
            ListingId = listing.ListingId,
            IsNew = isNew,
            FirstSeenDate = listing.FirstSeenDate
        };
    }
}
```

**PriceChangeDetectionService.cs** (Core/Services/)
```csharp
namespace AutomatedMarketIntelligenceTool.Core.Services;

public class PriceChangeDetectionService : IPriceChangeDetectionService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PriceChangeDetectionService> _logger;

    public async Task<PriceChangeResult?> DetectAndRecordPriceChangeAsync(
        Listing listing,
        decimal newPrice,
        CancellationToken cancellationToken = default)
    {
        if (listing.Price == newPrice)
            return null;

        var priceHistory = PriceHistory.Create(
            _tenantContext.TenantId,
            listing.ListingId,
            newPrice,
            listing.Price);

        _context.PriceHistory.Add(priceHistory);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Price change detected for {ListingId}: ${OldPrice} -> ${NewPrice} ({Change:+0.00;-0.00}%)",
            listing.ListingId, listing.Price, newPrice, priceHistory.ChangePercentage);

        listing.UpdatePrice(newPrice);

        return new PriceChangeResult
        {
            ListingId = listing.ListingId,
            OldPrice = listing.Price,
            NewPrice = newPrice,
            PriceChange = priceHistory.PriceChange,
            ChangePercentage = priceHistory.ChangePercentage
        };
    }
}
```

---

### Sprint 4: CLI Enhancements

#### Goals
- Implement list, export, and config commands
- Add comprehensive help documentation
- Implement progress bars and indicators

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S4-01 | Create ListCommand | Sprint 3 | P0 |
| S4-02 | Add pagination to ListCommand | S4-01 | P0 |
| S4-03 | Add all filter options to ListCommand | S4-01, Sprint 3 | P0 |
| S4-04 | Create ExportCommand | Sprint 3 | P0 |
| S4-05 | Implement JSON export | S4-04 | P0 |
| S4-06 | Implement CSV export | S4-04 | P0 |
| S4-07 | Create ConfigCommand | None | P1 |
| S4-08 | Implement config get operation | S4-07 | P1 |
| S4-09 | Implement config set operation | S4-07 | P1 |
| S4-10 | Create AppSettings configuration class | None | P0 |
| S4-11 | Add environment variable support | S4-10 | P1 |
| S4-12 | Add config file support | S4-10 | P1 |
| S4-13 | Implement global --help | None | P0 |
| S4-14 | Implement per-command --help | S4-13 | P0 |
| S4-15 | Add --version option | None | P0 |
| S4-16 | Implement progress bars (Spectre.Console) | None | P0 |
| S4-17 | Add colored output | None | P1 |
| S4-18 | Add --no-color option | S4-17 | P1 |
| S4-19 | Add verbosity options (-v, -q) | None | P1 |

#### Key Code Artifacts

**ListCommand.cs** (Cli/Commands/)
```csharp
namespace AutomatedMarketIntelligenceTool.Cli.Commands;

public class ListCommand : AsyncCommand<ListCommand.Settings>
{
    private readonly ISearchService _searchService;
    private readonly IOutputFormatter _formatter;

    public class Settings : CommandSettings
    {
        [CommandOption("-m|--make")]
        public string[]? Makes { get; set; }

        [CommandOption("--model")]
        public string[]? Models { get; set; }

        [CommandOption("--year-min")]
        public int? YearMin { get; set; }

        [CommandOption("--year-max")]
        public int? YearMax { get; set; }

        [CommandOption("--price-min")]
        public decimal? PriceMin { get; set; }

        [CommandOption("--price-max")]
        public decimal? PriceMax { get; set; }

        [CommandOption("--condition")]
        [Description("Filter by condition: new, used, cpo")]
        public string[]? Conditions { get; set; }

        [CommandOption("--transmission")]
        [Description("Filter by transmission: automatic, manual, cvt")]
        public string[]? Transmissions { get; set; }

        [CommandOption("--fuel-type")]
        [Description("Filter by fuel type: gasoline, diesel, electric, hybrid")]
        public string[]? FuelTypes { get; set; }

        [CommandOption("--body-style")]
        [Description("Filter by body style: sedan, suv, truck, coupe, etc.")]
        public string[]? BodyStyles { get; set; }

        [CommandOption("--city")]
        public string? City { get; set; }

        [CommandOption("--state")]
        public string? State { get; set; }

        [CommandOption("--new-only")]
        [Description("Show only new listings")]
        public bool NewOnly { get; set; }

        [CommandOption("--sort")]
        [Description("Sort by: price, mileage, year, date")]
        public string? SortBy { get; set; }

        [CommandOption("-f|--format")]
        [Description("Output format: table, json, csv")]
        public string Format { get; set; } = "table";

        [CommandOption("--page")]
        public int Page { get; set; } = 1;

        [CommandOption("--page-size")]
        public int PageSize { get; set; } = 25;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // Implementation
    }
}
```

**ExportCommand.cs** (Cli/Commands/)
```csharp
namespace AutomatedMarketIntelligenceTool.Cli.Commands;

public class ExportCommand : AsyncCommand<ExportCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<output-file>")]
        [Description("Output file path")]
        public string OutputFile { get; set; } = string.Empty;

        [CommandOption("-f|--format")]
        [Description("Export format: json, csv (auto-detected from extension if not specified)")]
        public string? Format { get; set; }

        // Include all filter options from ListCommand
        [CommandOption("-m|--make")]
        public string[]? Makes { get; set; }

        // ... additional filter options
    }
}
```

---

### Sprint 5: Reporting & Integration

#### Goals
- Implement new listing and price change highlighting
- Add sorting and filtering to output
- Complete integration testing

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S5-01 | Implement [NEW] indicator in table output | Sprint 3 | P0 |
| S5-02 | Add green color for new listings | S5-01 | P0 |
| S5-03 | Implement --new-only filter | S5-01 | P0 |
| S5-04 | Implement price change delta display | Sprint 3 | P0 |
| S5-05 | Add green/red color for price changes | S5-04 | P0 |
| S5-06 | Implement --sort option | None | P0 |
| S5-07 | Support multi-field sorting | S5-06 | P1 |
| S5-08 | Implement ascending/descending sort | S5-06 | P0 |
| S5-09 | Implement --filter option | None | P1 |
| S5-10 | Add column selection (--columns) | None | P1 |
| S5-11 | Implement search result summary | Sprint 3 | P0 |
| S5-12 | Add site breakdown in summary | S5-11 | P1 |
| S5-13 | Create E2E tests for scrape flow | All | P0 |
| S5-14 | Create E2E tests for search flow | All | P0 |
| S5-15 | Create E2E tests for export flow | All | P0 |
| S5-16 | Performance testing | All | P1 |
| S5-17 | Update user documentation | All | P1 |

#### Key Code Artifacts

**Enhanced TableFormatter Output**
```
┌────────────────────────────────────────────────────────────────────────────────┐
│                           Search Results Summary                                │
├────────────────────────────────────────────────────────────────────────────────┤
│ Total Listings: 127    New: 23    Price Changes: 8    Duration: 45.3s         │
│ Sources: Autotrader (52), Cars.com (43), CarGurus (32)                        │
└────────────────────────────────────────────────────────────────────────────────┘

┌──────┬──────────┬─────────┬────────────┬──────────┬─────────────┬──────────────┐
│ Year │ Make     │ Model   │    Price   │  Mileage │   Location  │    Source    │
├──────┼──────────┼─────────┼────────────┼──────────┼─────────────┼──────────────┤
│ 2023 │ Toyota   │ Camry   │   $28,500  │   12,345 │ Los Ange... │ Autotrader   │
│ 2022 │ Honda    │ Accord  │   $26,900  │   18,234 │ San Fran... │ Cars.com     │
│ 2023 │ Toyota   │ Camry   │   $27,200  │   15,678 │ San Diego   │ CarGurus     │[NEW]
│ 2021 │ Honda    │ Civic   │   $22,500  │   32,100 │ Phoenix     │ Autotrader   │↓$1,200 (-5.1%)
│ ...  │ ...      │ ...     │    ...     │    ...   │   ...       │ ...          │
└──────┴──────────┴─────────┴────────────┴──────────┴─────────────┴──────────────┘

Page 1 of 6 (25 of 127 listings)
```

---

## Technical Specifications

### Configuration Schema

```json
{
  "Database": {
    "Provider": "SqlServer | SQLite",
    "ConnectionString": "...",
    "SQLitePath": "~/.car-search/database.db"
  },
  "Scraping": {
    "DefaultDelayMs": 3000,
    "MaxRetries": 3,
    "MaxPages": 50,
    "DefaultSites": ["autotrader", "cars.com", "cargurus"],
    "HeadedMode": false
  },
  "Search": {
    "DefaultRadius": 25,
    "DefaultPageSize": 25
  },
  "Deactivation": {
    "StaleDays": 7
  },
  "Output": {
    "DefaultFormat": "table",
    "ColorEnabled": true
  }
}
```

### CLI Commands Reference (Phase 2)

```bash
# List saved listings
car-search list
car-search list -m Toyota --condition used --sort price
car-search list --new-only --page 2 --page-size 50

# Export listings
car-search export results.json -m Honda
car-search export results.csv --condition new --format csv

# Configuration
car-search config list
car-search config get Database:Provider
car-search config set Scraping:DefaultDelayMs 5000

# Scraping with site selection
car-search scrape --sites autotrader,cargurus -m Toyota
car-search scrape --exclude-sites cargurus -z 90210

# Search with extended filters
car-search search -m Toyota --condition used --transmission automatic
car-search search --fuel-type electric,hybrid --sort price:asc
car-search search --body-style suv,truck --city "Los Angeles" --state CA
```

### Error Codes (Extended)

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General error |
| 2 | Validation error (invalid arguments) |
| 3 | Network error |
| 4 | Database error |
| 5 | Scraping error |
| 6 | Rate limit exceeded |
| 7 | Configuration error |
| 8 | Export error |

---

## Risk Mitigation

### Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| SQL Server migration compatibility | Medium | High | Test migrations on both providers in CI |
| Rate limiting misconfiguration | Medium | Medium | Default to conservative delays, monitor logs |
| Extended field extraction failures | High | Medium | Graceful null handling, site-specific adapters |
| CarGurus DOM changes | High | Medium | Selector configuration files, monitoring |
| Performance degradation with filters | Low | Medium | Query optimization, index analysis |

### Mitigation Strategies

1. **Database Compatibility**: Create provider-agnostic integration tests that run on both SQLite and SQL Server
2. **Rate Limiting**: Implement adaptive rate limiting that increases delays on repeated 429 responses
3. **Field Extraction**: Log warnings for missing fields, never fail on optional data
4. **New Scraper Stability**: Add health check endpoint that validates selector configurations

---

## Definition of Done

### Feature Level
- [ ] All acceptance criteria from specs met
- [ ] Unit tests written and passing (>80% coverage for new code)
- [ ] Integration tests for database and scraper interactions
- [ ] Code reviewed and approved
- [ ] No security vulnerabilities
- [ ] Structured logging implemented
- [ ] Documentation updated

### Sprint Level
- [ ] All P0 tasks completed
- [ ] All P1 tasks completed or deferred with justification
- [ ] E2E tests passing
- [ ] Performance acceptable (<5s for 1000 result queries)
- [ ] Both SQLite and SQL Server tested

### Phase Level
- [ ] All sprints completed
- [ ] Full E2E flows working with extended filters
- [ ] SQL Server deployment documented
- [ ] User documentation complete with new commands
- [ ] Performance benchmarks documented

---

## Diagrams

### Architecture Diagrams
- [Phase 2 Architecture](./diagrams/phase-2-architecture.png)
- [Implementation Sequence](./diagrams/implementation-sequence.png)
- [Rate Limiting Flow](./diagrams/rate-limiting-flow.png)
- [Implementation Timeline](./diagrams/implementation-timeline.drawio.svg)

### Source Files
- [phase-2-architecture.puml](./diagrams/phase-2-architecture.puml)
- [implementation-sequence.puml](./diagrams/implementation-sequence.puml)
- [rate-limiting-flow.puml](./diagrams/rate-limiting-flow.puml)
- [implementation-timeline.drawio](./diagrams/implementation-timeline.drawio)

---

## Appendix

### References
- [Phase 1 Roadmap](../phase-1/README.md)
- [Implementation Specs](../../specs/implementation-specs.md)
- [CLI Interface Specs](../../specs/cli-interface/cli-interface.specs.md)
- [Web Scraping Specs](../../specs/web-scraping/web-scraping.specs.md)
- [Data Persistence Specs](../../specs/data-persistence/data-persistence.specs.md)
- [Duplicate Detection Specs](../../specs/duplicate-detection/duplicate-detection.specs.md)
- [Search Configuration Specs](../../specs/search-configuration/search-configuration.specs.md)
- [Location Configuration Specs](../../specs/location-configuration/location-configuration.specs.md)
- [Reporting Specs](../../specs/reporting/reporting.specs.md)

### Changelog
| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-08 | Claude | Initial roadmap creation |
