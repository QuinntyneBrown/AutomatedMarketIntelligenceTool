# Phase 5 Technical Implementation Roadmap

## Automated Market Intelligence Tool - Mature System

**Version:** 1.0
**Last Updated:** 2026-01-08
**Status:** Planning
**Prerequisites:** Phase 4 Complete

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Phase 5 Objectives](#phase-5-objectives)
3. [Implementation Philosophy](#implementation-philosophy)
4. [Architecture Evolution](#architecture-evolution)
5. [Implementation Sequence](#implementation-sequence)
6. [Sprint Breakdown](#sprint-breakdown)
7. [Technical Specifications](#technical-specifications)
8. [Risk Mitigation](#risk-mitigation)
9. [Definition of Done](#definition-of-done)

---

## Executive Summary

Phase 5 represents the mature, production-ready system with:
- Full 10+ scraper site ecosystem with complete coverage
- Advanced caching and performance optimizations
- HTML/PDF report generation for professional output
- Real-time dashboard view for monitoring
- Dealer reliability metrics and pattern analysis
- Advanced deduplication configuration and auditing
- Custom market region definitions
- Resource throttling for enterprise deployments

### Key Metrics

| Metric | Phase 4 | Phase 5 Target |
|--------|---------|----------------|
| Sites Supported | 10+ | 10+ (fully mature) |
| Report Formats | Table/JSON/CSV | + HTML/PDF/Excel |
| Caching | None | Request + Response |
| Performance | Baseline | Optimized (<20% dedup overhead) |
| Monitoring | Health checks | + Dashboard + Metrics |
| Automation | Alerts | + Scheduled reports |

### Build Order Summary

```
1. Performance Foundation (Caching, Batch Operations)
2. Full Scraper Ecosystem (Remaining sites, Header Config)
3. Advanced Reporting (HTML/PDF, Custom Templates)
4. Dashboard & Monitoring (Real-time View, Metrics)
5. Dealer & Pattern Analytics (Reliability, Relisting Patterns)
6. Deduplication Optimization (Performance, Audit, Config)
7. Enterprise Features (Custom Markets, Scheduling, Scaling)
```

---

## Phase 5 Objectives

### Primary Goals

| ID | Objective | Specs Coverage |
|----|-----------|----------------|
| P5-OBJ-01 | Request header configuration | REQ-WS-013 (AC-013.1-013.4) |
| P5-OBJ-02 | Response caching | REQ-WS-014 (AC-014.1-014.5) |
| P5-OBJ-03 | Resource throttling | REQ-WS-011 (AC-011.5) |
| P5-OBJ-04 | Relisting pattern tracking | REQ-DD-008 (AC-008.5) |
| P5-OBJ-05 | Dealer reliability metrics | REQ-DD-009 (AC-009.3-009.5) |
| P5-OBJ-06 | Deduplication performance | REQ-DD-010 (AC-010.1-010.5) |
| P5-OBJ-07 | Deduplication audit | REQ-DD-011 (AC-011.3-011.5) |
| P5-OBJ-08 | Deduplication configuration | REQ-DD-012 (AC-012.1-012.5) |
| P5-OBJ-09 | Report generation (HTML/PDF) | REQ-RP-013 (AC-013.1-013.5) |
| P5-OBJ-10 | Dashboard view | REQ-RP-014 (AC-014.1-014.5) |
| P5-OBJ-11 | Custom market regions | REQ-LC-008 (AC-008.5) |
| P5-OBJ-12 | Excel export | REQ-DP-009 (AC-009.3) |
| P5-OBJ-13 | Mobile user agents | REQ-WS-008 (AC-008.5) |

### Feature Requirements Matrix

| Feature Area | Requirements | Priority |
|--------------|--------------|----------|
| **Response Caching** | AC-014.1-014.5 | P0 |
| **Deduplication Performance** | AC-010.1-010.5 | P0 |
| **HTML Report** | AC-013.1, AC-013.3-013.5 | P0 |
| **PDF Report** | AC-013.2 | P1 |
| **Dashboard** | AC-014.1-014.5 | P1 |
| **Request Headers** | AC-013.1-013.4 | P1 |
| **Dealer Metrics** | AC-009.3-009.5 | P1 |
| **Relisting Patterns** | AC-008.5 | P2 |
| **Deduplication Audit** | AC-011.3-011.5 | P1 |
| **Custom Markets** | AC-008.5 | P2 |
| **Excel Export** | AC-009.3 | P2 |
| **Resource Throttling** | AC-011.5 | P2 |
| **Mobile UAs** | AC-008.5 | P2 |
| **Dedup Config** | AC-012.1-012.5 | P1 |
| **Batch Dedup** | AC-010.5 | P1 |

---

## Implementation Philosophy

### Build Order Rationale

**Why Performance Foundation First?**

```
┌──────────────────────────────────────────────────────────────────┐
│  1. PERFORMANCE FOUNDATION                                        │
│     ├── Response Caching Service                                  │
│     ├── Batch Deduplication Operations                           │
│     ├── Query Optimization (Blocking/Bucketing)                  │
│     └── Index Tuning                                             │
│                                                                    │
│  WHY FIRST: Performance underpins all mature system operations   │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  2. FULL SCRAPER ECOSYSTEM                                        │
│     ├── Remaining Site Scrapers (if any gaps)                    │
│     ├── Request Header Configuration                             │
│     ├── Mobile User Agent Support                                │
│     └── Site-Specific Optimizations                              │
│                                                                    │
│  WHY SECOND: Complete data coverage before analytics            │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  3. ADVANCED REPORTING                                            │
│     ├── HTML Report Generator                                     │
│     ├── PDF Report Generator                                      │
│     ├── Excel Export                                             │
│     └── Custom Report Templates                                  │
│                                                                    │
│  WHY THIRD: Professional output for mature usage                 │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  4. DASHBOARD & MONITORING                                        │
│     ├── Real-time Dashboard View                                  │
│     ├── Market Trends Summary                                     │
│     ├── Watch Mode (Auto-refresh)                                │
│     └── Comprehensive Metrics                                    │
│                                                                    │
│  WHY FOURTH: Monitoring for operational excellence               │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  5. DEALER & PATTERN ANALYTICS                                    │
│     ├── Dealer Reliability Scoring                               │
│     ├── Relisting Pattern Detection                              │
│     ├── Dealer-Specific Dedup Rules                              │
│     └── Inventory History Tracking                               │
│                                                                    │
│  WHY FIFTH: Advanced analytics on mature data                    │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  6. DEDUPLICATION OPTIMIZATION                                    │
│     ├── Audit Trail System                                        │
│     ├── Configuration Management                                  │
│     ├── False Positive Tracking                                  │
│     └── Accuracy Reporting                                       │
│                                                                    │
│  WHY SIXTH: Fine-tuning dedup for production accuracy            │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  7. ENTERPRISE FEATURES                                           │
│     ├── Custom Market Region Definitions                         │
│     ├── Scheduled Report Generation                              │
│     ├── Resource Scaling & Throttling                            │
│     └── Admin Multi-Tenant Management                            │
│                                                                    │
│  WHY LAST: Enterprise features for production deployments        │
└──────────────────────────────────────────────────────────────────┘
```

### Design Patterns Applied

| Pattern | Application | Rationale |
|---------|-------------|-----------|
| **Cache-Aside** | Response Caching | Check cache before fetch |
| **Template Method** | Report Generation | Common structure, format-specific rendering |
| **Singleton** | Dashboard State | Single source of truth |
| **Strategy Pattern** | Dedup Configuration | Interchangeable matching strategies |
| **Event Sourcing** | Audit Trail | Complete history of decisions |
| **Specification Pattern** | Market Definitions | Composable location criteria |

---

## Architecture Evolution

### Phase 5 Architecture Additions

```
src/
├── AutomatedMarketIntelligenceTool.Core/
│   ├── Models/
│   │   ├── DealerAggregate/
│   │   │   └── Dealer.cs                         [EXTENDED: Metrics]
│   │   ├── DeduplicationAuditAggregate/          [NEW]
│   │   │   ├── AuditEntry.cs
│   │   │   ├── AuditEntryId.cs
│   │   │   └── AuditReason.cs
│   │   ├── MarketRegionAggregate/                [NEW]
│   │   │   ├── MarketRegion.cs
│   │   │   ├── MarketRegionId.cs
│   │   │   └── RegionZipCode.cs
│   │   └── ReportAggregate/                      [NEW]
│   │       ├── Report.cs
│   │       ├── ReportId.cs
│   │       └── ReportTemplate.cs
│   └── Services/
│       ├── Deduplication/
│       │   ├── IDeduplicationConfigService.cs    [NEW]
│       │   ├── DeduplicationConfigService.cs     [NEW]
│       │   ├── IBatchDeduplicationService.cs     [NEW]
│       │   └── BatchDeduplicationService.cs      [NEW]
│       ├── Analytics/                            [NEW]
│       │   ├── IDealerAnalyticsService.cs
│       │   ├── DealerAnalyticsService.cs
│       │   ├── IRelistingPatternService.cs
│       │   └── RelistingPatternService.cs
│       ├── Reporting/                            [NEW]
│       │   ├── IReportGenerationService.cs
│       │   ├── HtmlReportGenerator.cs
│       │   ├── PdfReportGenerator.cs
│       │   └── ExcelReportGenerator.cs
│       └── Dashboard/                            [NEW]
│           ├── IDashboardService.cs
│           └── DashboardService.cs
│
├── AutomatedMarketIntelligenceTool.Infrastructure/
│   ├── EntityConfigurations/
│   │   ├── AuditEntryConfiguration.cs            [NEW]
│   │   ├── MarketRegionConfiguration.cs          [NEW]
│   │   └── ReportConfiguration.cs                [NEW]
│   ├── Services/
│   │   ├── Caching/                              [NEW]
│   │   │   ├── IResponseCacheService.cs
│   │   │   ├── ResponseCacheService.cs
│   │   │   ├── CacheConfiguration.cs
│   │   │   └── CacheCleanupService.cs
│   │   ├── Headers/                              [NEW]
│   │   │   ├── IHeaderConfigurationService.cs
│   │   │   └── HeaderConfigurationService.cs
│   │   └── Scheduling/                           [NEW]
│   │       ├── ISchedulingService.cs
│   │       └── SchedulingService.cs
│   └── Migrations/
│       └── Phase5Migration.cs
│
├── AutomatedMarketIntelligenceTool.Api/
│   ├── Features/
│   │   ├── Dashboard/                            [NEW]
│   │   │   ├── GetDashboardQuery.cs
│   │   │   └── DashboardDto.cs
│   │   ├── Reports/                              [NEW]
│   │   │   ├── GenerateReportCommand.cs
│   │   │   └── ReportDto.cs
│   │   ├── Audit/                                [NEW]
│   │   │   ├── GetAuditLogQuery.cs
│   │   │   └── AuditEntryDto.cs
│   │   └── Markets/                              [NEW]
│   │       ├── CreateMarketCommand.cs
│   │       └── MarketRegionDto.cs
│
└── AutomatedMarketIntelligenceTool.Cli/
    ├── Commands/
    │   ├── DashboardCommand.cs                   [NEW]
    │   ├── ReportCommand.cs                      [NEW]
    │   ├── MarketCommand.cs                      [NEW]
    │   ├── AuditCommand.cs                       [NEW]
    │   └── ScheduleCommand.cs                    [NEW]
    └── Templates/                                [NEW]
        ├── HtmlReportTemplate.html
        └── ExcelTemplate.xlsx
```

### Database Schema Evolution

```sql
-- New Tables

CREATE TABLE DeduplicationAudit (
    AuditEntryId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Listing1Id UNIQUEIDENTIFIER NOT NULL,
    Listing2Id UNIQUEIDENTIFIER,             -- Null if new listing
    Decision NVARCHAR(50) NOT NULL,          -- NewListing, Duplicate, NearMatch
    Reason NVARCHAR(50) NOT NULL,            -- VinMatch, FuzzyMatch, ImageMatch, Manual
    ConfidenceScore DECIMAL(5,2),
    WasAutomatic BIT NOT NULL,
    ManualOverride BIT NOT NULL DEFAULT 0,
    OverrideReason NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(100),

    INDEX IX_Audit_TenantId (TenantId),
    INDEX IX_Audit_Decision (Decision),
    INDEX IX_Audit_CreatedAt (CreatedAt)
);

CREATE TABLE MarketRegions (
    MarketRegionId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    IsSystemDefined BIT NOT NULL DEFAULT 0,
    CenterLatitude DECIMAL(9,6),
    CenterLongitude DECIMAL(9,6),
    RadiusMiles INT,
    CreatedAt DATETIME2 NOT NULL,

    CONSTRAINT UQ_Market_Name UNIQUE (TenantId, Name),
    INDEX IX_Market_TenantId (TenantId)
);

CREATE TABLE MarketRegionZipCodes (
    MarketRegionZipCodeId UNIQUEIDENTIFIER PRIMARY KEY,
    MarketRegionId UNIQUEIDENTIFIER NOT NULL,
    ZipCode NVARCHAR(10) NOT NULL,

    CONSTRAINT FK_MarketZip_Region FOREIGN KEY (MarketRegionId)
        REFERENCES MarketRegions(MarketRegionId) ON DELETE CASCADE,
    CONSTRAINT UQ_Market_ZipCode UNIQUE (MarketRegionId, ZipCode)
);

CREATE TABLE Reports (
    ReportId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Format NVARCHAR(20) NOT NULL,            -- HTML, PDF, Excel
    SearchCriteria NVARCHAR(MAX),            -- JSON
    FilePath NVARCHAR(500),
    FileSize BIGINT,
    Status NVARCHAR(20) NOT NULL,            -- Pending, Generating, Complete, Failed
    CreatedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2,
    ErrorMessage NVARCHAR(MAX),

    INDEX IX_Report_TenantId (TenantId),
    INDEX IX_Report_Status (Status)
);

CREATE TABLE ResponseCache (
    CacheKey NVARCHAR(500) PRIMARY KEY,
    Url NVARCHAR(2000) NOT NULL,
    ResponseData VARBINARY(MAX) NOT NULL,
    ContentType NVARCHAR(100),
    CachedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    HitCount INT NOT NULL DEFAULT 0,

    INDEX IX_Cache_ExpiresAt (ExpiresAt)
);

CREATE TABLE DeduplicationConfig (
    ConfigId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    ConfigKey NVARCHAR(100) NOT NULL,
    ConfigValue NVARCHAR(MAX) NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,

    CONSTRAINT UQ_DedupConfig_Key UNIQUE (TenantId, ConfigKey),
    INDEX IX_DedupConfig_TenantId (TenantId)
);

-- Schema Modifications

ALTER TABLE Dealers ADD
    ReliabilityScore DECIMAL(5,2) NULL,
    AvgDaysOnMarket INT NULL,
    TotalListingsHistorical INT NOT NULL DEFAULT 0,
    FrequentRelisterFlag BIT NOT NULL DEFAULT 0,
    LastAnalyzedAt DATETIME2;

-- Performance Indexes
CREATE INDEX IX_Listing_CreatedAt_TenantId ON Listings(CreatedAt, TenantId);
CREATE INDEX IX_Listing_Make_Model_Year_TenantId ON Listings(Make, Model, Year, TenantId);

-- Covering index for fuzzy matching candidate lookup
CREATE INDEX IX_Listing_FuzzyCandidate ON Listings(Make, Year, Price, TenantId)
    INCLUDE (Model, Mileage, Latitude, Longitude, LinkedVehicleId);
```

---

## Implementation Sequence

### Phase 5 Implementation Timeline

```
Sprint 1: Performance Foundation (Week 1-2)
    │
    ├── 1.1 Response Caching Service
    │       ├── Cache-aside pattern
    │       ├── TTL configuration (default 1hr)
    │       ├── Cache invalidation
    │       └── Size limits
    │
    ├── 1.2 Batch Deduplication
    │       ├── Bulk import processing
    │       ├── Parallel matching
    │       └── Progress reporting
    │
    ├── 1.3 Query Optimization
    │       ├── Blocking/bucketing strategy
    │       ├── Index analysis
    │       └── Covering indexes
    │
    └── 1.4 Performance Benchmarks
            ├── Baseline measurements
            └── Target: <20% dedup overhead

Sprint 2: Full Scraper Ecosystem (Week 3-4)
    │
    ├── 2.1 Request Header Configuration
    │       ├── Accept, Accept-Language headers
    │       ├── Custom header support
    │       ├── Referer header management
    │       └── DNT header option
    │
    ├── 2.2 Mobile User Agent Support
    │       ├── Mobile UA pool
    │       └── Mobile site scraping
    │
    ├── 2.3 Site-Specific Optimizations
    │       ├── Tuned selectors
    │       └── Rate limit calibration
    │
    └── 2.4 Scraper Maturity
            ├── Error recovery improvements
            └── Full field extraction validation

Sprint 3: Advanced Reporting (Week 5-6)
    │
    ├── 3.1 HTML Report Generator
    │       ├── Template engine
    │       ├── Responsive design
    │       ├── Search criteria display
    │       └── Statistics section
    │
    ├── 3.2 PDF Report Generator
    │       ├── PDF library integration
    │       ├── Page layout
    │       └── Chart embedding
    │
    ├── 3.3 Excel Export
    │       ├── XLSX generation
    │       ├── Multiple sheets
    │       └── Formatted cells
    │
    ├── 3.4 Custom Templates
    │       └── User-defined templates
    │
    └── 3.5 Report Command
            ├── Format selection
            └── Output path options

Sprint 4: Dashboard & Monitoring (Week 7-8)
    │
    ├── 4.1 Dashboard Service
    │       ├── Data aggregation
    │       ├── Trend calculation
    │       └── Watch list summary
    │
    ├── 4.2 Dashboard Command
    │       ├── Overview display
    │       ├── Watch mode (auto-refresh)
    │       └── Compact/detailed views
    │
    ├── 4.3 Market Trends
    │       ├── Price trends
    │       ├── Inventory trends
    │       └── New listings rate
    │
    └── 4.4 Real-time Metrics
            ├── Active searches
            ├── Recent alerts
            └── System health

Sprint 5: Dealer & Pattern Analytics (Week 9-10)
    │
    ├── 5.1 Dealer Reliability Scoring
    │       ├── Scoring algorithm
    │       ├── Historical analysis
    │       └── Reliability API
    │
    ├── 5.2 Relisting Pattern Detection
    │       ├── Pattern identification
    │       ├── Frequent relister flagging
    │       └── Notification system
    │
    ├── 5.3 Dealer-Specific Dedup Rules
    │       ├── Per-dealer thresholds
    │       └── Rule management
    │
    └── 5.4 Inventory History
            ├── Historical tracking
            └── Trend analysis

Sprint 6: Deduplication Optimization (Week 11-12)
    │
    ├── 6.1 Audit Trail System
    │       ├── Audit entity
    │       ├── Decision logging
    │       └── Query interface
    │
    ├── 6.2 Audit Command
    │       ├── View audit log
    │       └── Filter by decision type
    │
    ├── 6.3 Configuration Management
    │       ├── Threshold configuration
    │       ├── Weight configuration
    │       ├── Enable/disable features
    │       └── Strict mode (VIN-only)
    │
    ├── 6.4 False Positive Tracking
    │       └── From manual overrides
    │
    └── 6.5 Accuracy Reporting
            ├── Precision metrics
            └── Recall metrics

Sprint 7: Enterprise Features (Week 13-14)
    │
    ├── 7.1 Custom Market Regions
    │       ├── Market entity
    │       ├── ZIP code association
    │       └── Market command
    │
    ├── 7.2 Scheduled Reports
    │       ├── Schedule command
    │       ├── Cron integration
    │       └── Email delivery
    │
    ├── 7.3 Resource Throttling
    │       ├── CPU monitoring
    │       ├── Memory monitoring
    │       └── Adaptive throttling
    │
    ├── 7.4 Admin Features
    │       ├── Tenant management
    │       └── System configuration
    │
    └── 7.5 Final Integration
            ├── E2E testing
            ├── Performance validation
            └── Documentation completion
```

---

## Sprint Breakdown

### Sprint 1: Performance Foundation

#### Goals
- Implement response caching for reduced load
- Create batch deduplication for imports
- Optimize query performance

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S1-01 | Create IResponseCacheService interface | None | P0 |
| S1-02 | Implement ResponseCacheService | S1-01 | P0 |
| S1-03 | Create ResponseCache table | S1-01 | P0 |
| S1-04 | Add cache TTL configuration | S1-02 | P0 |
| S1-05 | Implement cache size limits | S1-02 | P1 |
| S1-06 | Create CacheCleanupService | S1-02 | P1 |
| S1-07 | Add --no-cache CLI option | S1-02 | P0 |
| S1-08 | Create IBatchDeduplicationService | None | P0 |
| S1-09 | Implement BatchDeduplicationService | S1-08 | P0 |
| S1-10 | Add blocking/bucketing optimization | S1-09 | P0 |
| S1-11 | Create covering indexes | None | P0 |
| S1-12 | Add performance benchmarks | S1-09 | P0 |
| S1-13 | Validate <20% overhead target | S1-12 | P0 |

#### Key Code Artifacts

**ResponseCacheService.cs** (Infrastructure/Services/Caching/)
```csharp
namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Caching;

public class ResponseCacheService : IResponseCacheService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly CacheConfiguration _config;
    private readonly ILogger<ResponseCacheService> _logger;

    public async Task<CacheResult<T>> GetOrFetchAsync<T>(
        string key,
        Func<Task<T>> fetchFunc,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTtl = ttl ?? TimeSpan.FromHours(_config.DefaultTtlHours);

        // Check cache
        var cached = await _context.ResponseCache
            .FirstOrDefaultAsync(c =>
                c.CacheKey == key &&
                c.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (cached != null)
        {
            cached.HitCount++;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Cache hit for key: {Key}", key);
            var data = JsonSerializer.Deserialize<T>(cached.ResponseData);
            return CacheResult<T>.Hit(data);
        }

        // Cache miss - fetch
        _logger.LogDebug("Cache miss for key: {Key}", key);
        var result = await fetchFunc();

        // Store in cache
        var serialized = JsonSerializer.SerializeToUtf8Bytes(result);

        if (serialized.Length <= _config.MaxEntrySizeBytes)
        {
            var entry = new ResponseCacheEntry
            {
                CacheKey = key,
                ResponseData = serialized,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(effectiveTtl)
            };

            _context.ResponseCache.Add(entry);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return CacheResult<T>.Miss(result);
    }

    public async Task InvalidateAsync(string keyPattern, CancellationToken cancellationToken = default)
    {
        var entries = await _context.ResponseCache
            .Where(c => EF.Functions.Like(c.CacheKey, keyPattern))
            .ToListAsync(cancellationToken);

        _context.ResponseCache.RemoveRange(entries);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

**BatchDeduplicationService.cs** (Core/Services/Deduplication/)
```csharp
namespace AutomatedMarketIntelligenceTool.Core.Services.Deduplication;

public class BatchDeduplicationService : IBatchDeduplicationService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly IDuplicateDetectionService _dupService;
    private readonly ILogger<BatchDeduplicationService> _logger;

    public async Task<BatchDeduplicationResult> ProcessBatchAsync(
        IEnumerable<ScrapedListing> listings,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var listingList = listings.ToList();
        var result = new BatchDeduplicationResult();
        var processed = 0;

        // Pre-fetch candidate blocks for efficiency
        var candidateBlocks = await PreFetchCandidateBlocksAsync(listingList, cancellationToken);

        // Process in parallel with limited concurrency
        await Parallel.ForEachAsync(
            listingList,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            },
            async (listing, ct) =>
            {
                var candidates = candidateBlocks.GetCandidates(listing.Make, listing.Year);
                var dupResult = await _dupService.CheckForDuplicateAsync(listing, candidates, ct);

                lock (result)
                {
                    if (dupResult.IsDuplicate)
                        result.DuplicateCount++;
                    else
                        result.NewListingCount++;
                }

                var currentProgress = Interlocked.Increment(ref processed);
                progress?.Report(new BatchProgress(currentProgress, listingList.Count));
            });

        return result;
    }

    private async Task<CandidateBlockIndex> PreFetchCandidateBlocksAsync(
        List<ScrapedListing> listings,
        CancellationToken cancellationToken)
    {
        // Get unique make/year combinations
        var makeYears = listings
            .Select(l => (l.Make?.ToUpperInvariant(), l.Year))
            .Distinct()
            .ToList();

        // Blocking: fetch candidates by make+year±2
        var candidates = await _context.Listings
            .Where(l => makeYears.Any(my =>
                l.Make.ToUpper() == my.Item1 &&
                l.Year >= my.Year - 2 &&
                l.Year <= my.Year + 2))
            .Select(l => new CandidateListing
            {
                ListingId = l.ListingId,
                Make = l.Make,
                Model = l.Model,
                Year = l.Year,
                Price = l.Price,
                Mileage = l.Mileage,
                Vin = l.Vin,
                Latitude = l.Latitude,
                Longitude = l.Longitude
            })
            .ToListAsync(cancellationToken);

        return new CandidateBlockIndex(candidates);
    }
}
```

---

### Sprint 2: Full Scraper Ecosystem

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S2-01 | Create IHeaderConfigurationService | None | P1 |
| S2-02 | Implement HeaderConfigurationService | S2-01 | P1 |
| S2-03 | Add Accept/Accept-Language headers | S2-02 | P1 |
| S2-04 | Add --header CLI option | S2-02 | P1 |
| S2-05 | Add Referer header management | S2-02 | P1 |
| S2-06 | Add DNT header option | S2-02 | P2 |
| S2-07 | Add mobile user agents to pool | None | P2 |
| S2-08 | Add mobile site detection | S2-07 | P2 |
| S2-09 | Tune all scraper selectors | None | P1 |
| S2-10 | Calibrate rate limits | None | P1 |
| S2-11 | Validate full field extraction | None | P1 |

---

### Sprint 3: Advanced Reporting

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S3-01 | Create IReportGenerationService | None | P0 |
| S3-02 | Implement HtmlReportGenerator | S3-01 | P0 |
| S3-03 | Create HTML template | S3-02 | P0 |
| S3-04 | Add search criteria section | S3-02 | P0 |
| S3-05 | Add statistics section | S3-02 | P0 |
| S3-06 | Add PDFsharp NuGet package | None | P1 |
| S3-07 | Implement PdfReportGenerator | S3-06 | P1 |
| S3-08 | Add ClosedXML NuGet package | None | P2 |
| S3-09 | Implement ExcelReportGenerator | S3-08 | P2 |
| S3-10 | Create ReportCommand | S3-02 | P0 |
| S3-11 | Add custom template support | S3-02 | P2 |

---

### Sprint 4: Dashboard & Monitoring

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S4-01 | Create IDashboardService | None | P1 |
| S4-02 | Implement DashboardService | S4-01 | P1 |
| S4-03 | Add data aggregation | S4-02 | P1 |
| S4-04 | Add trend calculations | S4-02 | P1 |
| S4-05 | Create DashboardCommand | S4-02 | P1 |
| S4-06 | Add overview display | S4-05 | P1 |
| S4-07 | Add watch mode (auto-refresh) | S4-05 | P2 |
| S4-08 | Add market trends section | S4-04 | P1 |
| S4-09 | Add real-time metrics | S4-02 | P1 |

---

### Sprint 5: Dealer & Pattern Analytics

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S5-01 | Create IDealerAnalyticsService | None | P1 |
| S5-02 | Implement DealerAnalyticsService | S5-01 | P1 |
| S5-03 | Implement reliability scoring | S5-02 | P1 |
| S5-04 | Add historical analysis | S5-02 | P1 |
| S5-05 | Create IRelistingPatternService | None | P2 |
| S5-06 | Implement RelistingPatternService | S5-05 | P2 |
| S5-07 | Add frequent relister flagging | S5-06 | P2 |
| S5-08 | Add dealer-specific dedup rules | S5-02 | P2 |
| S5-09 | Add inventory history tracking | S5-02 | P2 |

---

### Sprint 6: Deduplication Optimization

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S6-01 | Create AuditEntry entity | None | P1 |
| S6-02 | Create AuditEntryConfiguration | S6-01 | P1 |
| S6-03 | Integrate audit logging | S6-01 | P1 |
| S6-04 | Create AuditCommand | S6-01 | P1 |
| S6-05 | Create IDeduplicationConfigService | None | P1 |
| S6-06 | Implement DeduplicationConfigService | S6-05 | P1 |
| S6-07 | Add --dedup-threshold option | S6-06 | P1 |
| S6-08 | Add --no-dedup option | S6-06 | P1 |
| S6-09 | Add strict mode (VIN-only) | S6-06 | P1 |
| S6-10 | Track false positives from overrides | S6-03 | P1 |
| S6-11 | Add accuracy reporting | S6-10 | P1 |

---

### Sprint 7: Enterprise Features

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S7-01 | Create MarketRegion entity | None | P2 |
| S7-02 | Create MarketRegionConfiguration | S7-01 | P2 |
| S7-03 | Seed system-defined markets | S7-02 | P2 |
| S7-04 | Create MarketCommand | S7-02 | P2 |
| S7-05 | Create ScheduleCommand | None | P2 |
| S7-06 | Add cron syntax generator | S7-05 | P2 |
| S7-07 | Add email notification for reports | None | P2 |
| S7-08 | Add resource throttling | None | P2 |
| S7-09 | Add adaptive throttling | S7-08 | P2 |
| S7-10 | E2E testing | All | P0 |
| S7-11 | Performance validation | All | P0 |
| S7-12 | Complete documentation | All | P1 |

---

## Technical Specifications

### Performance Targets

| Operation | Phase 4 | Phase 5 Target |
|-----------|---------|----------------|
| Single listing dedup | 50ms | 40ms |
| Batch dedup (1000) | 60s | 12s |
| Search query | 200ms | 150ms |
| Dashboard load | N/A | <2s |
| Report generation | N/A | <30s |

### CLI Commands Reference (Phase 5)

```bash
# Dashboard
car-search dashboard
car-search dashboard --watch
car-search dashboard --compact

# Reports
car-search report --format html -o report.html -m Toyota
car-search report --format pdf -o market-report.pdf
car-search report --format excel -o listings.xlsx
car-search report --template custom-template.html

# Market regions
car-search market create "southern-california" --zip 90210,90211,...
car-search market list
car-search market delete "southern-california"
car-search search --market "southern-california"

# Audit
car-search audit list
car-search audit list --decision duplicate
car-search audit list --from 2026-01-01 --to 2026-01-31

# Deduplication configuration
car-search config set dedup.threshold 90
car-search config set dedup.strict-mode true
car-search config set dedup.enabled false
car-search config list dedup.*

# Scheduling
car-search schedule "daily-search" --cron "0 6 * * *" \
    -m Toyota --price-max 30000 --notify email@example.com

# Cache management
car-search cache clear
car-search cache stats
car-search scrape --no-cache

# Headers
car-search scrape --header "Accept-Language: en-US"
```

### Configuration Schema (Final)

```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "...",
    "CommandTimeout": 30
  },
  "Caching": {
    "Enabled": true,
    "DefaultTtlHours": 1,
    "MaxEntrySizeBytes": 10485760,
    "MaxTotalSizeMB": 500,
    "CleanupIntervalMinutes": 15
  },
  "Deduplication": {
    "Enabled": true,
    "AutoThreshold": 85,
    "ReviewThreshold": 60,
    "StrictMode": false,
    "ImageMatchingEnabled": true,
    "ImageMatchWeight": 0.10,
    "FieldWeights": {
      "Make": 0.15,
      "Model": 0.20,
      "Year": 0.15,
      "Mileage": 0.15,
      "Price": 0.20,
      "Location": 0.15
    }
  },
  "Scraping": {
    "DefaultConcurrency": 3,
    "MaxConcurrency": 10,
    "DefaultDelayMs": 3000,
    "ResourceThrottling": {
      "Enabled": true,
      "MaxCpuPercent": 80,
      "MaxMemoryMB": 2048
    }
  },
  "Reporting": {
    "OutputDirectory": "~/.car-search/reports",
    "DefaultFormat": "html",
    "IncludeStatistics": true
  },
  "Dashboard": {
    "RefreshIntervalSeconds": 30,
    "TrendDays": 30
  },
  "Markets": {
    "SystemDefined": [
      {
        "Name": "southern-california",
        "ZipCodes": ["90210", "90001", "91001", ...]
      },
      {
        "Name": "new-york-metro",
        "ZipCodes": ["10001", "10002", "07001", ...]
      }
    ]
  }
}
```

---

## Risk Mitigation

### Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Cache inconsistency | Medium | Medium | TTL + manual invalidation |
| Performance degradation at scale | Medium | High | Continuous benchmarking |
| Report generation timeouts | Medium | Medium | Async generation + streaming |
| Dealer analytics accuracy | Medium | Low | Conservative scoring, manual validation |

### Mitigation Strategies

1. **Cache Strategy**: Implement cache-aside with conservative TTLs; always allow bypass
2. **Performance**: Establish CI performance tests; alert on regression
3. **Reporting**: Background job queue for large reports; progress indication
4. **Analytics**: Mark metrics as "experimental"; allow user override

---

## Definition of Done

### Feature Level
- [ ] All acceptance criteria met
- [ ] Performance targets achieved
- [ ] Caching reduces load by 50%+
- [ ] Reports generate correctly

### Sprint Level
- [ ] All P0 tasks completed
- [ ] All P1 tasks completed or deferred
- [ ] Performance benchmarks passing
- [ ] No regression in existing features

### Phase Level
- [ ] All sprints completed
- [ ] Full 10+ site ecosystem mature
- [ ] Dashboard operational
- [ ] Reports (HTML/PDF/Excel) working
- [ ] Deduplication <20% overhead
- [ ] Production deployment documentation complete

---

## Diagrams

### Architecture Diagrams
- [Phase 5 Architecture](./diagrams/phase-5-architecture.png)
- [Caching Strategy](./diagrams/caching-strategy.png)
- [Report Generation](./diagrams/report-generation.png)
- [Implementation Timeline](./diagrams/implementation-timeline.drawio.svg)

### Source Files
- [phase-5-architecture.puml](./diagrams/phase-5-architecture.puml)
- [caching-strategy.puml](./diagrams/caching-strategy.puml)
- [report-generation.puml](./diagrams/report-generation.puml)
- [implementation-timeline.drawio](./diagrams/implementation-timeline.drawio)

---

## Appendix

### References
- [Phase 1 Roadmap](../phase-1/README.md)
- [Phase 2 Roadmap](../phase-2/README.md)
- [Phase 3 Roadmap](../phase-3/README.md)
- [Phase 4 Roadmap](../phase-4/README.md)
- [All Feature Specifications](../../specs/)

### System Maturity Checklist

| Category | Phase 1-4 | Phase 5 |
|----------|-----------|---------|
| Scraping | Functional | Optimized |
| Deduplication | Accurate | Performant + Audited |
| CLI | Feature Complete | Polished |
| Reporting | Basic | Professional |
| Monitoring | Health Checks | Dashboard |
| Performance | Baseline | Optimized |
| Enterprise | Single User | Multi-tenant Ready |

### Changelog
| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-08 | Claude | Initial roadmap creation |
