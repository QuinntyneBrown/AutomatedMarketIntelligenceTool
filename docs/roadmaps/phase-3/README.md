# Phase 3 Technical Implementation Roadmap

## Automated Market Intelligence Tool - Advanced Features

**Version:** 1.0
**Last Updated:** 2026-01-08
**Status:** Planning
**Prerequisites:** Phase 2 Complete

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Phase 3 Objectives](#phase-3-objectives)
3. [Implementation Philosophy](#implementation-philosophy)
4. [Architecture Evolution](#architecture-evolution)
5. [Implementation Sequence](#implementation-sequence)
6. [Sprint Breakdown](#sprint-breakdown)
7. [Technical Specifications](#technical-specifications)
8. [Risk Mitigation](#risk-mitigation)
9. [Definition of Done](#definition-of-done)

---

## Executive Summary

Phase 3 delivers advanced features for sophisticated users:
- Fuzzy matching for cross-source duplicate detection
- Support for 10+ automotive listing sites
- Browser engine selection and proxy support
- User agent rotation and session management
- Advanced search filters (color, drivetrain, keywords, seller type)
- Search profile persistence
- Price history and market statistics
- Interactive CLI mode

### Key Metrics

| Metric | Phase 2 | Phase 3 Target |
|--------|---------|----------------|
| Sites Supported | 4-5 | 10+ |
| Search Filters | 12 | 18+ |
| Duplicate Detection | VIN + ExternalId | + Fuzzy Matching |
| Database Providers | SQLite + SQL Server | + PostgreSQL |
| CLI Commands | 5 | 8+ |
| Confidence Scoring | None | 0-100% |

### Build Order Summary

```
1. Infrastructure (PostgreSQL, Proxy, User Agent)
2. Advanced Scraping (Browser Selection, Session Management, New Sites)
3. Fuzzy Matching Engine (Confidence Scoring, Cross-Source Linking)
4. Extended Search (Colors, Drivetrain, Keywords, Seller Type)
5. Profile & Configuration (Save/Load Profiles, Verbosity)
6. Statistics & Analysis (Market Stats, Price History Display)
7. Interactive Mode & Polish
```

---

## Phase 3 Objectives

### Primary Goals

| ID | Objective | Specs Coverage |
|----|-----------|----------------|
| P3-OBJ-01 | Fuzzy matching with confidence scoring | REQ-DD-003 (AC-003.1-003.7) |
| P3-OBJ-02 | Cross-source vehicle linking | REQ-DD-005 (AC-005.1-005.2) |
| P3-OBJ-03 | 10+ scraper sites support | REQ-WS-001 (AC-001.1-001.2) |
| P3-OBJ-04 | Browser engine selection | REQ-WS-002 (AC-002.2) |
| P3-OBJ-05 | Proxy support | REQ-WS-007 (AC-007.1-007.3, AC-007.6) |
| P3-OBJ-06 | User agent management | REQ-WS-008 (AC-008.1-008.4) |
| P3-OBJ-07 | Session management | REQ-WS-009 (AC-009.1-009.5) |
| P3-OBJ-08 | Color filtering | REQ-SC-010 (AC-010.1-010.4) |
| P3-OBJ-09 | Drivetrain filtering | REQ-SC-011 (AC-011.1-011.4) |
| P3-OBJ-10 | Search profile persistence | REQ-SC-012 (AC-012.1-012.6) |
| P3-OBJ-11 | Keyword search | REQ-SC-013 (AC-013.1-013.4) |
| P3-OBJ-12 | Seller type filter | REQ-SC-014 (AC-014.1-014.3) |
| P3-OBJ-13 | Statistics report | REQ-RP-007 (AC-007.1-007.6) |
| P3-OBJ-14 | Price analysis | REQ-RP-008 (AC-008.1-008.4) |
| P3-OBJ-15 | Interactive mode | REQ-CLI-006 (AC-006.1-006.6) |
| P3-OBJ-16 | PostgreSQL support | REQ-DP-001 (AC-001.3) |

### Feature Requirements Matrix

| Feature Area | Requirements | Priority |
|--------------|--------------|----------|
| **Fuzzy Matching** | AC-003.1-003.7 | P0 |
| **Cross-Source Linking** | AC-005.1-005.2 | P0 |
| **Browser Selection** | AC-002.2 | P1 |
| **Proxy Support** | AC-007.1-007.3, AC-007.6 | P1 |
| **User Agent Rotation** | AC-008.1-008.4 | P1 |
| **Session Management** | AC-009.1-009.5 | P2 |
| **Color Filter** | AC-010.1-010.4 | P0 |
| **Drivetrain Filter** | AC-011.1-011.4 | P0 |
| **Keyword Search** | AC-013.1-013.4 | P1 |
| **Seller Type Filter** | AC-014.1-014.3 | P1 |
| **Search Profiles** | AC-012.1-012.6 | P1 |
| **Statistics Report** | AC-007.1-007.6 | P1 |
| **Price Analysis** | AC-008.1-008.4 | P1 |
| **Interactive Mode** | AC-006.1-006.6 | P2 |
| **PostgreSQL** | AC-001.3 | P2 |
| **Multiple Locations** | AC-005.1-005.5 | P2 |
| **Saved Locations** | AC-007.1-007.5 | P2 |

---

## Implementation Philosophy

### Build Order Rationale

**Why Fuzzy Matching First?**

```
┌──────────────────────────────────────────────────────────────────┐
│  1. INFRASTRUCTURE EXTENSIONS                                     │
│     ├── PostgreSQL Provider                                       │
│     ├── Proxy Configuration Service                               │
│     ├── User Agent Rotation Service                               │
│     └── Browser Context Factory                                   │
│                                                                    │
│  WHY FIRST: New scrapers need proxy/UA infrastructure            │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  2. ADVANCED SCRAPING                                             │
│     ├── Browser Engine Selection (Chromium/Firefox/WebKit)        │
│     ├── Session Management Service                                │
│     ├── 6+ Additional Scrapers                                    │
│     │   ├── eBay Motors                                          │
│     │   ├── CarMax                                               │
│     │   ├── Carvana                                              │
│     │   ├── Vroom                                                │
│     │   ├── TrueCar                                              │
│     │   └── Craigslist (regional)                                │
│     └── Robots.txt Compliance                                     │
│                                                                    │
│  WHY SECOND: More data sources = better fuzzy matching accuracy  │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  3. FUZZY MATCHING ENGINE                                         │
│     ├── Similarity Algorithms (Make/Model/Year/Price/Location)    │
│     ├── Confidence Score Calculator                               │
│     ├── Configurable Thresholds                                   │
│     ├── Vehicle Entity for Cross-Source Linking                   │
│     └── Partial VIN Matching                                      │
│                                                                    │
│  WHY THIRD: Requires multi-source data for meaningful matching   │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  4. EXTENDED SEARCH CAPABILITIES                                  │
│     ├── Color Filtering (Exterior/Interior)                       │
│     ├── Drivetrain Filtering (FWD/RWD/AWD/4WD)                   │
│     ├── Keyword Search                                           │
│     ├── Seller Type Filter                                       │
│     └── Coordinate-Based Location                                │
│                                                                    │
│  WHY FOURTH: Filters work on expanded scraped data               │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  5. PROFILE & CONFIGURATION                                       │
│     ├── Search Profile Save/Load                                  │
│     ├── Profile Command                                          │
│     ├── Status Command                                           │
│     ├── Multiple Location Search                                 │
│     └── Saved Locations                                          │
│                                                                    │
│  WHY FIFTH: User convenience features after core functionality   │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  6. STATISTICS & ANALYSIS                                         │
│     ├── Market Statistics Report                                  │
│     ├── Price Analysis (Avg, Median, Deal Rating)                │
│     ├── Listing Detail View                                      │
│     └── Price History Display                                    │
│                                                                    │
│  WHY SIXTH: Analytics require sufficient data volume             │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  7. INTERACTIVE MODE & POLISH                                     │
│     ├── Interactive Query Builder                                 │
│     ├── Auto-Completion for Make/Model                           │
│     ├── Advanced Verbosity Levels                                │
│     └── Non-TTY Output Handling                                  │
│                                                                    │
│  WHY LAST: UX polish after all features functional               │
└──────────────────────────────────────────────────────────────────┘
```

### Design Patterns Applied

| Pattern | Application | Rationale |
|---------|-------------|-----------|
| **Strategy Pattern** | Fuzzy Matching Algorithms | Different similarity measures per field |
| **Composite Pattern** | Confidence Score Calculator | Combine multiple similarity scores |
| **Factory Pattern** | Browser Context Factory | Create browser instances per engine |
| **Proxy Pattern** | Proxy Configuration | Intercept and route web requests |
| **Template Method** | Site Scrapers | Common structure, site-specific parsing |
| **Memento Pattern** | Search Profiles | Save and restore search configurations |

### Fuzzy Matching Algorithm Design

```
┌─────────────────────────────────────────────────────────────────────┐
│                    FUZZY MATCHING PIPELINE                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Input: New Scraped Listing                                         │
│                                                                      │
│  Step 1: VIN Check (if available)                                   │
│          ├── Full VIN match → 100% confidence, EXACT_MATCH          │
│          └── Last 8 chars match → 95% confidence, PARTIAL_VIN       │
│                                                                      │
│  Step 2: ExternalId + SourceSite Check                              │
│          └── Match → 100% confidence, SAME_SOURCE                   │
│                                                                      │
│  Step 3: Fuzzy Attribute Matching                                   │
│          ├── Make:     Levenshtein distance, weight 15%             │
│          ├── Model:    Levenshtein distance, weight 20%             │
│          ├── Year:     Exact match = 1.0, ±1 = 0.8, weight 15%      │
│          ├── Mileage:  ±500 miles = 1.0, scaled, weight 15%         │
│          ├── Price:    ±$500 = 1.0, scaled, weight 20%              │
│          └── Location: ≤10 miles = 1.0, scaled, weight 15%          │
│                                                                      │
│  Step 4: Calculate Composite Score                                   │
│          Score = Σ(field_score × field_weight) × 100                │
│                                                                      │
│  Step 5: Apply Threshold                                            │
│          ├── Score ≥ 85% → Automatic deduplication                  │
│          ├── 60% ≤ Score < 85% → Flag for review (Phase 4)          │
│          └── Score < 60% → Treat as new listing                     │
│                                                                      │
│  Output: DuplicateCheckResult with confidence score                 │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Architecture Evolution

### Phase 3 Architecture Additions

```
src/
├── AutomatedMarketIntelligenceTool.Core/
│   ├── Models/
│   │   ├── ListingAggregate/
│   │   │   └── Listing.cs                        [EXTENDED: LinkedVehicleId]
│   │   ├── VehicleAggregate/                     [NEW]
│   │   │   ├── Vehicle.cs
│   │   │   ├── VehicleId.cs
│   │   │   └── Events/
│   │   │       └── VehicleLinked.cs
│   │   └── SearchProfileAggregate/               [NEW]
│   │       ├── SearchProfile.cs
│   │       ├── SearchProfileId.cs
│   │       └── SavedLocation.cs
│   └── Services/
│       ├── FuzzyMatching/                        [NEW]
│       │   ├── IFuzzyMatchingService.cs
│       │   ├── FuzzyMatchingService.cs
│       │   ├── ISimilarityCalculator.cs
│       │   ├── LevenshteinCalculator.cs
│       │   ├── NumericProximityCalculator.cs
│       │   ├── GeoDistanceCalculator.cs
│       │   └── ConfidenceScoreCalculator.cs
│       ├── IVehicleLinkingService.cs             [NEW]
│       ├── VehicleLinkingService.cs              [NEW]
│       ├── ISearchProfileService.cs              [NEW]
│       ├── SearchProfileService.cs               [NEW]
│       ├── IStatisticsService.cs                 [NEW]
│       └── StatisticsService.cs                  [NEW]
│
├── AutomatedMarketIntelligenceTool.Infrastructure/
│   ├── EntityConfigurations/
│   │   ├── VehicleConfiguration.cs               [NEW]
│   │   └── SearchProfileConfiguration.cs         [NEW]
│   ├── Services/
│   │   ├── Proxy/                                [NEW]
│   │   │   ├── IProxyService.cs
│   │   │   ├── ProxyService.cs
│   │   │   └── ProxyConfiguration.cs
│   │   ├── UserAgent/                            [NEW]
│   │   │   ├── IUserAgentService.cs
│   │   │   ├── UserAgentService.cs
│   │   │   └── UserAgentPool.cs
│   │   ├── Browser/                              [NEW]
│   │   │   ├── IBrowserContextFactory.cs
│   │   │   └── BrowserContextFactory.cs
│   │   ├── Session/                              [NEW]
│   │   │   ├── ISessionManager.cs
│   │   │   └── SessionManager.cs
│   │   └── Scrapers/
│   │       ├── EbayMotorsScraper.cs              [NEW]
│   │       ├── CarMaxScraper.cs                  [NEW]
│   │       ├── CarvanaScraper.cs                 [NEW]
│   │       ├── VroomScraper.cs                   [NEW]
│   │       ├── TrueCarScraper.cs                 [NEW]
│   │       └── CraigslistScraper.cs              [NEW]
│   └── Migrations/
│       └── Phase3Migration.cs
│
├── AutomatedMarketIntelligenceTool.Api/
│   ├── Features/
│   │   ├── Statistics/                           [NEW]
│   │   │   ├── GetMarketStatisticsQuery.cs
│   │   │   └── MarketStatisticsDto.cs
│   │   ├── Profiles/                             [NEW]
│   │   │   ├── SaveProfileCommand.cs
│   │   │   ├── LoadProfileQuery.cs
│   │   │   └── SearchProfileDto.cs
│   │   └── Vehicles/                             [NEW]
│   │       ├── GetVehicleWithListingsQuery.cs
│   │       └── VehicleDto.cs
│   └── Extensions/
│       ├── VehicleExtensions.cs                  [NEW]
│       └── SearchProfileExtensions.cs            [NEW]
│
└── AutomatedMarketIntelligenceTool.Cli/
    ├── Commands/
    │   ├── ProfileCommand.cs                     [NEW]
    │   ├── StatusCommand.cs                      [NEW]
    │   ├── StatsCommand.cs                       [NEW]
    │   └── ShowCommand.cs                        [NEW]
    └── Interactive/                              [NEW]
        ├── InteractiveMode.cs
        ├── MakeModelAutoComplete.cs
        └── PromptHelper.cs
```

### Database Schema Evolution

```sql
-- New Tables

CREATE TABLE Vehicles (
    VehicleId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    PrimaryVin NVARCHAR(17),              -- Best known VIN
    Make NVARCHAR(100) NOT NULL,
    Model NVARCHAR(100) NOT NULL,
    Year INT NOT NULL,
    Trim NVARCHAR(100),
    BestPrice DECIMAL(12,2),              -- Lowest current price
    AveragePrice DECIMAL(12,2),           -- Average across sources
    ListingCount INT NOT NULL DEFAULT 0,
    FirstSeenDate DATETIME2 NOT NULL,
    LastSeenDate DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2,

    INDEX IX_Vehicle_TenantId (TenantId),
    INDEX IX_Vehicle_Vin (PrimaryVin),
    INDEX IX_Vehicle_MakeModel (Make, Model, Year)
);

CREATE TABLE SearchProfiles (
    SearchProfileId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    SearchParameters NVARCHAR(MAX) NOT NULL,  -- JSON serialized
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2,
    LastUsedAt DATETIME2,

    CONSTRAINT UQ_Profile_Name UNIQUE (TenantId, Name),
    INDEX IX_Profile_TenantId (TenantId)
);

CREATE TABLE SavedLocations (
    SavedLocationId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    ZipCode NVARCHAR(10),
    City NVARCHAR(100),
    State NVARCHAR(50),
    Latitude DECIMAL(9,6),
    Longitude DECIMAL(9,6),
    DefaultRadius INT NOT NULL DEFAULT 25,
    CreatedAt DATETIME2 NOT NULL,

    CONSTRAINT UQ_Location_Name UNIQUE (TenantId, Name),
    INDEX IX_Location_TenantId (TenantId)
);

-- Schema Modifications

ALTER TABLE Listings ADD
    LinkedVehicleId UNIQUEIDENTIFIER NULL,
    MatchConfidence DECIMAL(5,2) NULL,
    MatchMethod NVARCHAR(50) NULL,
    Keywords NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Listing_Vehicle
        FOREIGN KEY (LinkedVehicleId) REFERENCES Vehicles(VehicleId);

-- New Indexes for Fuzzy Matching
CREATE INDEX IX_Listing_LinkedVehicle ON Listings(LinkedVehicleId);
CREATE INDEX IX_Listing_MakeModelYear ON Listings(Make, Model, Year);
CREATE INDEX IX_Listing_ExteriorColor ON Listings(ExteriorColor);
CREATE INDEX IX_Listing_InteriorColor ON Listings(InteriorColor);
CREATE INDEX IX_Listing_Drivetrain ON Listings(Drivetrain);
CREATE INDEX IX_Listing_SellerType ON Listings(SellerType);
```

---

## Implementation Sequence

### Phase 3 Implementation Timeline

```
Sprint 1: Infrastructure Extensions (Week 1-2)
    │
    ├── 1.1 PostgreSQL Provider
    │       ├── Connection configuration
    │       ├── Provider factory update
    │       └── Migration compatibility
    │
    ├── 1.2 Proxy Configuration Service
    │       ├── HTTP/HTTPS proxy support
    │       ├── SOCKS5 proxy support
    │       └── Proxy authentication
    │
    ├── 1.3 User Agent Service
    │       ├── User agent pool
    │       ├── Rotation logic
    │       └── Browser engine matching
    │
    └── 1.4 Browser Context Factory
            ├── Chromium/Firefox/WebKit support
            └── Context configuration

Sprint 2: Advanced Scraping (Week 3-4)
    │
    ├── 2.1 Browser Engine Selection
    │       ├── --browser option
    │       ├── Engine-specific configurations
    │       └── User agent matching
    │
    ├── 2.2 Session Management
    │       ├── Session persistence
    │       ├── Per-site session isolation
    │       └── Session cleanup
    │
    ├── 2.3 New Scrapers (Set 1)
    │       ├── eBay Motors
    │       ├── CarMax
    │       └── Carvana
    │
    └── 2.4 New Scrapers (Set 2)
            ├── Vroom
            ├── TrueCar
            └── Craigslist (regional)

Sprint 3: Fuzzy Matching Engine (Week 5-6)
    │
    ├── 3.1 Similarity Calculators
    │       ├── Levenshtein distance (Make/Model)
    │       ├── Numeric proximity (Year/Mileage/Price)
    │       └── Geo distance (Location)
    │
    ├── 3.2 Confidence Score Calculator
    │       ├── Weighted composite scoring
    │       ├── Configurable weights
    │       └── Threshold management
    │
    ├── 3.3 Fuzzy Matching Service
    │       ├── Integration with duplicate detection
    │       ├── Partial VIN matching
    │       └── Match method recording
    │
    └── 3.4 Vehicle Linking Service
            ├── Vehicle entity creation
            ├── Cross-source linking
            └── Best price calculation

Sprint 4: Extended Search (Week 7-8)
    │
    ├── 4.1 Color Filtering
    │       ├── Exterior color filter
    │       ├── Interior color filter
    │       └── Color normalization
    │
    ├── 4.2 Drivetrain Filter
    │       ├── FWD/RWD/AWD/4WD options
    │       └── AWD/4WD equivalence
    │
    ├── 4.3 Keyword Search
    │       ├── Description search
    │       ├── Keyword extraction
    │       └── Special character escaping
    │
    ├── 4.4 Seller Type Filter
    │       └── Dealer/Private/Both options
    │
    └── 4.5 Coordinate-Based Location
            ├── --lat/--lon options
            └── Coordinate to ZIP conversion

Sprint 5: Profile & Configuration (Week 9-10)
    │
    ├── 5.1 Search Profile Entity
    │       ├── Profile save
    │       ├── Profile load
    │       └── Profile listing
    │
    ├── 5.2 Profile Command
    │       ├── save subcommand
    │       ├── load subcommand
    │       ├── list subcommand
    │       └── delete subcommand
    │
    ├── 5.3 Status Command
    │       ├── Scraper health display
    │       └── Statistics summary
    │
    ├── 5.4 Multiple Location Search
    │       └── Aggregated results
    │
    └── 5.5 Saved Locations
            ├── Location save/load
            └── Location management

Sprint 6: Statistics & Analysis (Week 11-12)
    │
    ├── 6.1 Statistics Service
    │       ├── Average/Median price
    │       ├── Price range
    │       ├── Mileage statistics
    │       └── Count by attribute
    │
    ├── 6.2 Stats Command
    │       ├── Market statistics display
    │       └── Filter-based statistics
    │
    ├── 6.3 Price Analysis
    │       ├── Deal rating calculation
    │       ├── Price per mile
    │       └── Market comparison
    │
    ├── 6.4 Show Command
    │       ├── Detailed listing view
    │       ├── Price history display
    │       └── Source link
    │
    └── 6.5 Price History Enhancements
            └── Visual trend indicators

Sprint 7: Interactive Mode & Polish (Week 13-14)
    │
    ├── 7.1 Interactive Mode Framework
    │       ├── Prompt-based flow
    │       ├── Parameter selection
    │       └── Command preview
    │
    ├── 7.2 Auto-Completion
    │       ├── Make suggestions
    │       ├── Model suggestions
    │       └── Location suggestions
    │
    ├── 7.3 Advanced Verbosity
    │       ├── -vv debug output
    │       ├── -vvv trace output
    │       └── Log file integration
    │
    ├── 7.4 Non-TTY Handling
    │       └── Simplified progress output
    │
    └── 7.5 Integration & Testing
            ├── E2E fuzzy matching tests
            ├── Multi-site scraping tests
            └── Documentation updates
```

---

## Sprint Breakdown

### Sprint 1: Infrastructure Extensions

#### Goals
- Add PostgreSQL as third database provider
- Implement proxy support for web requests
- Create user agent rotation service
- Build browser context factory for engine selection

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S1-01 | Add PostgreSQL NuGet package | None | P2 |
| S1-02 | Create PostgreSQL connection configuration | S1-01 | P2 |
| S1-03 | Update database provider factory | S1-02 | P2 |
| S1-04 | Create IProxyService interface | None | P1 |
| S1-05 | Implement ProxyService with HTTP/HTTPS | S1-04 | P1 |
| S1-06 | Add SOCKS5 proxy support | S1-05 | P1 |
| S1-07 | Add proxy authentication | S1-05 | P1 |
| S1-08 | Create IUserAgentService interface | None | P1 |
| S1-09 | Implement UserAgentPool | S1-08 | P1 |
| S1-10 | Implement rotation logic | S1-09 | P1 |
| S1-11 | Create IBrowserContextFactory interface | None | P0 |
| S1-12 | Implement Chromium context creation | S1-11 | P0 |
| S1-13 | Implement Firefox context creation | S1-11 | P1 |
| S1-14 | Implement WebKit context creation | S1-11 | P1 |
| S1-15 | Add --browser CLI option | S1-11 | P1 |
| S1-16 | Add --proxy CLI option | S1-05 | P1 |

#### Key Code Artifacts

**IFuzzyMatchingService.cs** (Core/Services/FuzzyMatching/)
```csharp
namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

public interface IFuzzyMatchingService
{
    Task<FuzzyMatchResult> FindBestMatchAsync(
        ScrapedListing listing,
        CancellationToken cancellationToken = default);

    decimal CalculateConfidenceScore(
        Listing existing,
        ScrapedListing incoming);
}

public record FuzzyMatchResult
{
    public Listing? MatchedListing { get; init; }
    public decimal ConfidenceScore { get; init; }
    public MatchMethod Method { get; init; }
    public Dictionary<string, decimal> FieldScores { get; init; } = new();
}

public enum MatchMethod
{
    None,
    ExactVin,
    PartialVin,
    ExternalId,
    FuzzyAttributes
}
```

**ConfidenceScoreCalculator.cs** (Core/Services/FuzzyMatching/)
```csharp
namespace AutomatedMarketIntelligenceTool.Core.Services.FuzzyMatching;

public class ConfidenceScoreCalculator
{
    private readonly ILevenshteinCalculator _levenshtein;
    private readonly INumericProximityCalculator _numeric;
    private readonly IGeoDistanceCalculator _geo;

    private static readonly Dictionary<string, decimal> FieldWeights = new()
    {
        ["Make"] = 0.15m,
        ["Model"] = 0.20m,
        ["Year"] = 0.15m,
        ["Mileage"] = 0.15m,
        ["Price"] = 0.20m,
        ["Location"] = 0.15m
    };

    public FuzzyMatchResult Calculate(Listing existing, ScrapedListing incoming)
    {
        var scores = new Dictionary<string, decimal>
        {
            ["Make"] = _levenshtein.Calculate(existing.Make, incoming.Make),
            ["Model"] = _levenshtein.Calculate(existing.Model, incoming.Model),
            ["Year"] = _numeric.Calculate(existing.Year, incoming.Year, tolerance: 1),
            ["Mileage"] = _numeric.Calculate(
                existing.Mileage ?? 0,
                incoming.Mileage ?? 0,
                tolerance: 500),
            ["Price"] = _numeric.Calculate(
                existing.Price,
                incoming.Price,
                tolerance: 500),
            ["Location"] = _geo.Calculate(
                existing.Latitude, existing.Longitude,
                incoming.Latitude, incoming.Longitude,
                toleranceMiles: 10)
        };

        var compositeScore = scores
            .Sum(kvp => kvp.Value * FieldWeights[kvp.Key]) * 100;

        return new FuzzyMatchResult
        {
            ConfidenceScore = Math.Round(compositeScore, 2),
            FieldScores = scores,
            Method = MatchMethod.FuzzyAttributes
        };
    }
}
```

---

### Sprint 2: Advanced Scraping

#### Goals
- Implement browser engine selection
- Add session management for persistent cookies
- Create 6 new site scrapers

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S2-01 | Update BaseScraper for browser selection | Sprint 1 | P0 |
| S2-02 | Create ISessionManager interface | None | P1 |
| S2-03 | Implement SessionManager | S2-02 | P1 |
| S2-04 | Add --persist-session option | S2-03 | P2 |
| S2-05 | Add --clear-session option | S2-03 | P2 |
| S2-06 | Create eBay Motors scraper | Sprint 1 | P0 |
| S2-07 | Create CarMax scraper | Sprint 1 | P0 |
| S2-08 | Create Carvana scraper | Sprint 1 | P0 |
| S2-09 | Create Vroom scraper | Sprint 1 | P1 |
| S2-10 | Create TrueCar scraper | Sprint 1 | P1 |
| S2-11 | Create Craigslist scraper | Sprint 1 | P1 |
| S2-12 | Implement robots.txt compliance | None | P1 |
| S2-13 | Add scraper health checks | S2-06-11 | P2 |
| S2-14 | Add integration tests | S2-06-11 | P1 |

---

### Sprint 3: Fuzzy Matching Engine

#### Goals
- Implement similarity calculation algorithms
- Build confidence score calculator
- Create vehicle linking service

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S3-01 | Create ISimilarityCalculator interface | None | P0 |
| S3-02 | Implement LevenshteinCalculator | S3-01 | P0 |
| S3-03 | Implement NumericProximityCalculator | S3-01 | P0 |
| S3-04 | Implement GeoDistanceCalculator | S3-01 | P0 |
| S3-05 | Create ConfidenceScoreCalculator | S3-02, S3-03, S3-04 | P0 |
| S3-06 | Create IFuzzyMatchingService | S3-05 | P0 |
| S3-07 | Implement FuzzyMatchingService | S3-06 | P0 |
| S3-08 | Implement partial VIN matching (last 8) | S3-07 | P0 |
| S3-09 | Create Vehicle entity | None | P0 |
| S3-10 | Create VehicleConfiguration | S3-09 | P0 |
| S3-11 | Create IVehicleLinkingService | S3-09 | P0 |
| S3-12 | Implement VehicleLinkingService | S3-11 | P0 |
| S3-13 | Integrate fuzzy matching into scrape flow | S3-07 | P0 |
| S3-14 | Add confidence threshold configuration | S3-07 | P1 |
| S3-15 | Add fuzzy matching unit tests | S3-07 | P0 |

---

### Sprint 4: Extended Search

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S4-01 | Implement exterior color filter | None | P0 |
| S4-02 | Implement interior color filter | None | P0 |
| S4-03 | Add color normalization (grey/gray) | S4-01, S4-02 | P0 |
| S4-04 | Implement drivetrain filter | None | P0 |
| S4-05 | Add AWD/4WD equivalence handling | S4-04 | P1 |
| S4-06 | Implement keyword search | None | P1 |
| S4-07 | Add keyword extraction from listings | S4-06 | P2 |
| S4-08 | Implement seller type filter | None | P1 |
| S4-09 | Add coordinate-based location (--lat/--lon) | None | P1 |
| S4-10 | Implement coordinate to ZIP conversion | S4-09 | P2 |
| S4-11 | Add filter unit tests | All | P1 |

---

### Sprint 5: Profile & Configuration

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S5-01 | Create SearchProfile entity | None | P1 |
| S5-02 | Create SearchProfileConfiguration | S5-01 | P1 |
| S5-03 | Create ISearchProfileService | S5-01 | P1 |
| S5-04 | Implement SearchProfileService | S5-03 | P1 |
| S5-05 | Create ProfileCommand | S5-04 | P1 |
| S5-06 | Implement profile save subcommand | S5-05 | P1 |
| S5-07 | Implement profile load subcommand | S5-05 | P1 |
| S5-08 | Implement profile list subcommand | S5-05 | P1 |
| S5-09 | Implement profile delete subcommand | S5-05 | P1 |
| S5-10 | Create StatusCommand | Sprint 2 | P2 |
| S5-11 | Create SavedLocation entity | None | P2 |
| S5-12 | Implement multiple location search | S5-11 | P2 |
| S5-13 | Add location save/load commands | S5-11 | P2 |

---

### Sprint 6: Statistics & Analysis

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S6-01 | Create IStatisticsService interface | None | P1 |
| S6-02 | Implement StatisticsService | S6-01 | P1 |
| S6-03 | Calculate average/median price | S6-02 | P1 |
| S6-04 | Calculate mileage statistics | S6-02 | P1 |
| S6-05 | Calculate count by attribute | S6-02 | P1 |
| S6-06 | Create StatsCommand | S6-02 | P1 |
| S6-07 | Implement deal rating calculation | S6-02 | P1 |
| S6-08 | Implement price per mile calculation | S6-02 | P2 |
| S6-09 | Create ShowCommand | None | P1 |
| S6-10 | Add detailed listing view | S6-09 | P1 |
| S6-11 | Add price history display | S6-09 | P1 |
| S6-12 | Add statistics unit tests | S6-02 | P1 |

---

### Sprint 7: Interactive Mode & Polish

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S7-01 | Create InteractiveMode framework | None | P2 |
| S7-02 | Implement parameter prompts | S7-01 | P2 |
| S7-03 | Add command preview | S7-01 | P2 |
| S7-04 | Create MakeModelAutoComplete | None | P2 |
| S7-05 | Implement make suggestions | S7-04 | P2 |
| S7-06 | Implement model suggestions | S7-04 | P2 |
| S7-07 | Add -vv debug output | None | P1 |
| S7-08 | Add -vvv trace output | S7-07 | P2 |
| S7-09 | Implement log file integration | S7-07 | P1 |
| S7-10 | Add non-TTY progress handling | None | P1 |
| S7-11 | Create E2E fuzzy matching tests | All | P0 |
| S7-12 | Create multi-site scraping tests | All | P0 |
| S7-13 | Update documentation | All | P1 |

---

## Technical Specifications

### Fuzzy Matching Configuration

```json
{
  "FuzzyMatching": {
    "Enabled": true,
    "AutoDeduplicationThreshold": 85,
    "ReviewThreshold": 60,
    "FieldWeights": {
      "Make": 0.15,
      "Model": 0.20,
      "Year": 0.15,
      "Mileage": 0.15,
      "Price": 0.20,
      "Location": 0.15
    },
    "Tolerances": {
      "MileageMiles": 500,
      "PriceDollars": 500,
      "LocationMiles": 10,
      "YearRange": 1
    },
    "PartialVinLength": 8
  },
  "Proxy": {
    "Enabled": false,
    "Address": null,
    "Type": "HTTP",
    "Username": null,
    "Password": null
  },
  "UserAgent": {
    "RotationEnabled": true,
    "Pool": [
      "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36...",
      "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)..."
    ]
  },
  "Browser": {
    "Engine": "Chromium",
    "HeadedMode": false
  }
}
```

### CLI Commands Reference (Phase 3)

```bash
# Profile management
car-search profile save "family-suv" -m Toyota,Honda --body-style suv --price-max 40000
car-search profile load "family-suv"
car-search profile list
car-search profile delete "family-suv"

# Scraper status
car-search status
car-search status --site autotrader

# Statistics
car-search stats -m Toyota
car-search stats --condition used --year-min 2020

# Show listing details
car-search show <listing-id>
car-search show <listing-id> --with-history

# Extended search filters
car-search search --exterior-color black,white --interior-color tan
car-search search --drivetrain awd
car-search search --seller-type dealer
car-search search --keywords "leather seats, sunroof"

# Interactive mode
car-search search --interactive
car-search search -i

# Browser selection
car-search scrape --browser firefox
car-search scrape --browser webkit

# Proxy support
car-search scrape --proxy http://proxy:8080
car-search scrape --proxy socks5://user:pass@proxy:1080

# Location enhancements
car-search search --lat 34.0522 --lon -118.2437 --radius 50
car-search search --zip 90210,90211,90212

# Saved locations
car-search location save "home" --zip 90210 --radius 25
car-search location list
car-search search --location home
```

---

## Risk Mitigation

### Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Fuzzy matching false positives | Medium | High | Conservative threshold (85%), review queue |
| New scraper instability | High | Medium | Health monitoring, fallback to stable scrapers |
| Proxy blocking | Medium | Medium | Proxy rotation (Phase 4), user notification |
| PostgreSQL migration issues | Low | Medium | Thorough provider-agnostic testing |
| Interactive mode complexity | Medium | Low | Progressive enhancement, fallback to CLI |

### Mitigation Strategies

1. **Fuzzy Matching Quality**: Implement extensive unit tests with edge cases; allow manual override of auto-deduplicated listings
2. **Scraper Stability**: Health check system that tracks success rates; automatic fallback when site returns errors
3. **Proxy Support**: Clear error messages when proxy fails; automatic retry without proxy
4. **Database Compatibility**: Create migration tests that run on all three providers

---

## Definition of Done

### Feature Level
- [ ] All acceptance criteria from specs met
- [ ] Unit tests for fuzzy matching algorithms (>90% coverage)
- [ ] Integration tests for all new scrapers
- [ ] Confidence scoring validated against test dataset
- [ ] Structured logging for matching decisions

### Sprint Level
- [ ] All P0 tasks completed
- [ ] All P1 tasks completed or deferred
- [ ] 10+ scrapers functional
- [ ] Fuzzy matching accuracy >95% on test data
- [ ] All database providers tested

### Phase Level
- [ ] All sprints completed
- [ ] Cross-source linking operational
- [ ] Statistics and analysis features working
- [ ] Interactive mode functional
- [ ] Performance acceptable (<10s for fuzzy matching 1000 listings)

---

## Diagrams

### Architecture Diagrams
- [Phase 3 Architecture](./diagrams/phase-3-architecture.png)
- [Fuzzy Matching Flow](./diagrams/fuzzy-matching-flow.png)
- [Vehicle Linking](./diagrams/vehicle-linking.png)
- [Implementation Timeline](./diagrams/implementation-timeline.drawio.svg)

### Source Files
- [phase-3-architecture.puml](./diagrams/phase-3-architecture.puml)
- [fuzzy-matching-flow.puml](./diagrams/fuzzy-matching-flow.puml)
- [vehicle-linking.puml](./diagrams/vehicle-linking.puml)
- [implementation-timeline.drawio](./diagrams/implementation-timeline.drawio)

---

## Appendix

### References
- [Phase 1 Roadmap](../phase-1/README.md)
- [Phase 2 Roadmap](../phase-2/README.md)
- [Implementation Specs](../../specs/implementation-specs.md)
- [Duplicate Detection Specs](../../specs/duplicate-detection/duplicate-detection.specs.md)
- [Web Scraping Specs](../../specs/web-scraping/web-scraping.specs.md)
- [Search Configuration Specs](../../specs/search-configuration/search-configuration.specs.md)

### Changelog
| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-08 | Claude | Initial roadmap creation |
