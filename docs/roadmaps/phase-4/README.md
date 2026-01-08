# Phase 4 Technical Implementation Roadmap

## Automated Market Intelligence Tool - Image Analysis & Advanced Operations

**Version:** 1.0
**Last Updated:** 2026-01-08
**Status:** Planning
**Prerequisites:** Phase 3 Complete

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Phase 4 Objectives](#phase-4-objectives)
3. [Implementation Philosophy](#implementation-philosophy)
4. [Architecture Evolution](#architecture-evolution)
5. [Implementation Sequence](#implementation-sequence)
6. [Sprint Breakdown](#sprint-breakdown)
7. [Technical Specifications](#technical-specifications)
8. [Risk Mitigation](#risk-mitigation)
9. [Definition of Done](#definition-of-done)

---

## Executive Summary

Phase 4 introduces advanced capabilities for power users and enterprise deployments:
- Image-based duplicate detection using perceptual hashing
- Near-match review workflow for 60-85% confidence matches
- Watch list for tracking interesting listings
- Listing comparison view
- Alert system for automated notifications
- Concurrent multi-site scraping for performance
- Scraper health monitoring and diagnostics
- Data import/export and backup/restore
- Shell completion and signal handling

### Key Metrics

| Metric | Phase 3 | Phase 4 Target |
|--------|---------|----------------|
| Duplicate Detection | VIN + Fuzzy | + Image Hashing |
| Match Confidence | Auto only | Auto + Manual Review |
| Scraping Mode | Sequential | Concurrent (3 sites) |
| User Features | Search/Stats | + Watch/Compare/Alerts |
| Data Operations | Export | + Import/Backup/Restore |
| CLI Completeness | 80% | 95% |

### Build Order Summary

```
1. Image Analysis Infrastructure (Perceptual Hashing)
2. Near-Match Review System (Manual Resolution)
3. Concurrent Scraping Engine (Performance)
4. Scraper Health Monitoring (Reliability)
5. User Features (Watch List, Comparison, Alerts)
6. Data Operations (Import, Backup, Restore)
7. CLI Polish (Completion, Signals, Confirmations)
```

---

## Phase 4 Objectives

### Primary Goals

| ID | Objective | Specs Coverage |
|----|-----------|----------------|
| P4-OBJ-01 | Image-based duplicate detection | REQ-DD-004 (AC-004.1-004.5) |
| P4-OBJ-02 | Near-match flagging and review | REQ-DD-003 (AC-003.4), REQ-DD-006 |
| P4-OBJ-03 | Cross-source data aggregation | REQ-DD-005 (AC-005.3-005.5) |
| P4-OBJ-04 | Relisted vehicle detection | REQ-DD-008 (AC-008.1-008.4) |
| P4-OBJ-05 | Dealer inventory tracking | REQ-DD-009 (AC-009.1-009.2) |
| P4-OBJ-06 | Price history trends | REQ-RP-008 (AC-008.5) |
| P4-OBJ-07 | Watch list | REQ-RP-010 (AC-010.1-010.6) |
| P4-OBJ-08 | Comparison view | REQ-RP-011 (AC-011.1-011.5) |
| P4-OBJ-09 | Alert configuration | REQ-RP-012 (AC-012.1-012.5) |
| P4-OBJ-10 | Concurrent scraping | REQ-WS-011 (AC-011.1-011.4) |
| P4-OBJ-11 | Screenshot/debug capture | REQ-WS-010 (AC-010.1-010.5) |
| P4-OBJ-12 | Scraper health monitoring | REQ-WS-012 (AC-012.1-012.5) |
| P4-OBJ-13 | Data import | REQ-DP-010 (AC-010.1-010.5) |
| P4-OBJ-14 | Database backup/restore | REQ-DP-011 (AC-011.1-011.5) |
| P4-OBJ-15 | Shell completion | REQ-CLI-011 (AC-011.1-011.5) |
| P4-OBJ-16 | Signal handling | REQ-CLI-012 (AC-012.1-012.6) |
| P4-OBJ-17 | Confirmation prompts | REQ-CLI-010 (AC-010.1-010.5) |
| P4-OBJ-18 | Proxy rotation | REQ-WS-007 (AC-007.4-007.5) |

### Feature Requirements Matrix

| Feature Area | Requirements | Priority |
|--------------|--------------|----------|
| **Image Matching** | AC-004.1-004.5 | P0 |
| **Near-Match Review** | AC-003.4, AC-006.1-006.5 | P0 |
| **Relisted Detection** | AC-008.1-008.4 | P1 |
| **Concurrent Scraping** | AC-011.1-011.4 | P0 |
| **Health Monitoring** | AC-012.1-012.5 | P1 |
| **Watch List** | AC-010.1-010.6 | P1 |
| **Comparison View** | AC-011.1-011.5 | P1 |
| **Alert System** | AC-012.1-012.5 | P1 |
| **Data Import** | AC-010.1-010.5 | P1 |
| **Backup/Restore** | AC-011.1-011.5 | P1 |
| **Shell Completion** | AC-011.1-011.5 | P2 |
| **Signal Handling** | AC-012.1-012.6 | P1 |
| **Confirmations** | AC-010.1-010.5 | P2 |
| **Proxy Rotation** | AC-007.4-007.5 | P2 |
| **Screenshot Capture** | AC-010.1-010.5 | P2 |
| **Streamed Export** | AC-009.6 | P2 |

---

## Implementation Philosophy

### Build Order Rationale

**Why Image Analysis First?**

```
┌──────────────────────────────────────────────────────────────────┐
│  1. IMAGE ANALYSIS INFRASTRUCTURE                                 │
│     ├── Perceptual Hash Algorithm (pHash/dHash)                  │
│     ├── Image Download Service                                    │
│     ├── Hash Storage in Listing Entity                           │
│     └── Image Similarity Calculator                              │
│                                                                    │
│  WHY FIRST: Enables enhanced duplicate detection for all         │
│             subsequent matching operations                        │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  2. NEAR-MATCH REVIEW SYSTEM                                      │
│     ├── Review Queue Entity                                       │
│     ├── Manual Resolution Service                                 │
│     ├── Resolution Persistence                                    │
│     └── Review Command                                           │
│                                                                    │
│  WHY SECOND: Handles edge cases from fuzzy + image matching      │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  3. CONCURRENT SCRAPING ENGINE                                    │
│     ├── Parallel Site Execution                                   │
│     ├── Resource Management                                       │
│     ├── Result Aggregation                                        │
│     └── Concurrency Configuration                                │
│                                                                    │
│  WHY THIRD: Performance critical for 10+ sites                   │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  4. SCRAPER HEALTH MONITORING                                     │
│     ├── Success Rate Tracking                                     │
│     ├── Missing Element Detection                                 │
│     ├── Health Status API                                         │
│     └── Alert on Degradation                                     │
│                                                                    │
│  WHY FOURTH: Required for reliable concurrent operation          │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  5. USER FEATURES                                                 │
│     ├── Watch List (tracking favorites)                          │
│     ├── Comparison View (side-by-side)                           │
│     ├── Alert Configuration (notifications)                      │
│     └── Relisted Vehicle Detection                               │
│                                                                    │
│  WHY FIFTH: User value features built on stable core             │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  6. DATA OPERATIONS                                               │
│     ├── CSV/JSON Import with Validation                          │
│     ├── Database Backup Command                                  │
│     ├── Database Restore Command                                 │
│     └── Streamed Export for Large Data                           │
│                                                                    │
│  WHY SIXTH: Data management for mature system                    │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│  7. CLI POLISH                                                    │
│     ├── Shell Completion (Bash/Zsh/PowerShell)                   │
│     ├── Graceful Signal Handling (SIGINT)                        │
│     ├── Confirmation Prompts (destructive ops)                   │
│     └── Dry-Run Mode                                             │
│                                                                    │
│  WHY LAST: Polish features for production readiness              │
└──────────────────────────────────────────────────────────────────┘
```

### Design Patterns Applied

| Pattern | Application | Rationale |
|---------|-------------|-----------|
| **Producer-Consumer** | Concurrent Scraping | Thread-safe site processing |
| **Circuit Breaker** | Health Monitoring | Prevent cascade failures |
| **Command Pattern** | Alert Actions | Encapsulate notification logic |
| **Observer Pattern** | Watch List | React to listing changes |
| **Pipeline Pattern** | Image Processing | Sequential transformation stages |
| **Retry Pattern** | Connection Resilience | Handle transient failures |

### Image-Based Matching Algorithm

```
┌─────────────────────────────────────────────────────────────────────┐
│                    IMAGE MATCHING PIPELINE                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Input: Listing with ImageUrls                                      │
│                                                                      │
│  Step 1: Download Primary Images                                    │
│          ├── Download first 3 images from listing                   │
│          ├── Resize to 256x256 for consistency                     │
│          └── Handle download failures gracefully                    │
│                                                                      │
│  Step 2: Calculate Perceptual Hashes                                │
│          ├── Convert to grayscale                                   │
│          ├── Resize to 32x32 (DCT input)                           │
│          ├── Apply DCT transform                                    │
│          ├── Extract low-frequency 8x8 block                        │
│          └── Generate 64-bit hash per image                        │
│                                                                      │
│  Step 3: Compare Against Existing                                   │
│          ├── Query listings with existing hashes                    │
│          ├── Calculate Hamming distance                             │
│          └── Distance ≤ 10 bits = likely same image                 │
│                                                                      │
│  Step 4: Aggregate Match Confidence                                 │
│          ├── Require majority (2/3) images match                   │
│          ├── Image match adds +10% to fuzzy score                  │
│          └── Image-only match capped at 75%                        │
│                                                                      │
│  Output: Enhanced confidence score with image factor               │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Architecture Evolution

### Phase 4 Architecture Additions

```
src/
├── AutomatedMarketIntelligenceTool.Core/
│   ├── Models/
│   │   ├── ListingAggregate/
│   │   │   ├── Listing.cs                        [EXTENDED: ImageHashes]
│   │   │   └── Events/
│   │   │       ├── ListingWatched.cs             [NEW]
│   │   │       └── ListingRelisted.cs            [NEW]
│   │   ├── ReviewQueueAggregate/                 [NEW]
│   │   │   ├── ReviewItem.cs
│   │   │   ├── ReviewItemId.cs
│   │   │   ├── ResolutionDecision.cs
│   │   │   └── Events/
│   │   │       └── ReviewResolved.cs
│   │   ├── WatchListAggregate/                   [NEW]
│   │   │   ├── WatchedListing.cs
│   │   │   └── WatchedListingId.cs
│   │   ├── AlertAggregate/                       [NEW]
│   │   │   ├── Alert.cs
│   │   │   ├── AlertId.cs
│   │   │   ├── AlertCriteria.cs
│   │   │   └── AlertNotification.cs
│   │   └── DealerAggregate/                      [NEW]
│   │       ├── Dealer.cs
│   │       └── DealerId.cs
│   └── Services/
│       ├── ImageAnalysis/                        [NEW]
│       │   ├── IImageHashingService.cs
│       │   ├── ImageHashingService.cs
│       │   ├── IImageDownloadService.cs
│       │   ├── ImageDownloadService.cs
│       │   └── PerceptualHashCalculator.cs
│       ├── IReviewService.cs                     [NEW]
│       ├── ReviewService.cs                      [NEW]
│       ├── IWatchListService.cs                  [NEW]
│       ├── WatchListService.cs                   [NEW]
│       ├── IAlertService.cs                      [NEW]
│       ├── AlertService.cs                       [NEW]
│       ├── IRelistedDetectionService.cs          [NEW]
│       ├── RelistedDetectionService.cs           [NEW]
│       ├── IDealerTrackingService.cs             [NEW]
│       └── DealerTrackingService.cs              [NEW]
│
├── AutomatedMarketIntelligenceTool.Infrastructure/
│   ├── EntityConfigurations/
│   │   ├── ReviewItemConfiguration.cs            [NEW]
│   │   ├── WatchedListingConfiguration.cs        [NEW]
│   │   ├── AlertConfiguration.cs                 [NEW]
│   │   └── DealerConfiguration.cs                [NEW]
│   ├── Services/
│   │   ├── Scraping/                             [NEW]
│   │   │   ├── IConcurrentScrapingEngine.cs
│   │   │   ├── ConcurrentScrapingEngine.cs
│   │   │   └── ScrapingResourceManager.cs
│   │   ├── Health/                               [NEW]
│   │   │   ├── IScraperHealthService.cs
│   │   │   ├── ScraperHealthService.cs
│   │   │   └── HealthMetrics.cs
│   │   ├── Backup/                               [NEW]
│   │   │   ├── IBackupService.cs
│   │   │   ├── BackupService.cs
│   │   │   └── BackupConfiguration.cs
│   │   ├── Import/                               [NEW]
│   │   │   ├── IDataImportService.cs
│   │   │   ├── DataImportService.cs
│   │   │   ├── CsvImporter.cs
│   │   │   └── JsonImporter.cs
│   │   ├── Notifications/                        [NEW]
│   │   │   ├── INotificationService.cs
│   │   │   ├── ConsoleNotificationService.cs
│   │   │   ├── EmailNotificationService.cs
│   │   │   └── WebhookNotificationService.cs
│   │   └── Proxy/
│   │       └── ProxyRotationService.cs           [NEW]
│   └── Migrations/
│       └── Phase4Migration.cs
│
├── AutomatedMarketIntelligenceTool.Api/
│   ├── Features/
│   │   ├── Review/                               [NEW]
│   │   │   ├── GetReviewQueueQuery.cs
│   │   │   ├── ResolveReviewCommand.cs
│   │   │   └── ReviewItemDto.cs
│   │   ├── WatchList/                            [NEW]
│   │   │   ├── AddToWatchListCommand.cs
│   │   │   ├── GetWatchListQuery.cs
│   │   │   └── WatchedListingDto.cs
│   │   ├── Alerts/                               [NEW]
│   │   │   ├── CreateAlertCommand.cs
│   │   │   ├── GetAlertsQuery.cs
│   │   │   └── AlertDto.cs
│   │   └── Health/                               [NEW]
│   │       ├── GetScraperHealthQuery.cs
│   │       └── ScraperHealthDto.cs
│
└── AutomatedMarketIntelligenceTool.Cli/
    ├── Commands/
    │   ├── ReviewCommand.cs                      [NEW]
    │   ├── WatchCommand.cs                       [NEW]
    │   ├── CompareCommand.cs                     [NEW]
    │   ├── AlertCommand.cs                       [NEW]
    │   ├── ImportCommand.cs                      [NEW]
    │   ├── BackupCommand.cs                      [NEW]
    │   ├── RestoreCommand.cs                     [NEW]
    │   └── CompletionCommand.cs                  [NEW]
    ├── Completion/                               [NEW]
    │   ├── BashCompletionGenerator.cs
    │   ├── ZshCompletionGenerator.cs
    │   └── PowerShellCompletionGenerator.cs
    └── SignalHandling/                           [NEW]
        ├── GracefulShutdownHandler.cs
        └── PartialResultsSaver.cs
```

### Database Schema Evolution

```sql
-- New Tables

CREATE TABLE ReviewQueue (
    ReviewItemId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Listing1Id UNIQUEIDENTIFIER NOT NULL,
    Listing2Id UNIQUEIDENTIFIER NOT NULL,
    ConfidenceScore DECIMAL(5,2) NOT NULL,
    MatchMethod NVARCHAR(50) NOT NULL,
    FieldScores NVARCHAR(MAX),              -- JSON
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',  -- Pending, Resolved, Dismissed
    Resolution NVARCHAR(20),                 -- SameVehicle, DifferentVehicle
    ResolvedAt DATETIME2,
    ResolvedBy NVARCHAR(100),
    Notes NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL,

    CONSTRAINT FK_Review_Listing1 FOREIGN KEY (Listing1Id) REFERENCES Listings(ListingId),
    CONSTRAINT FK_Review_Listing2 FOREIGN KEY (Listing2Id) REFERENCES Listings(ListingId),
    INDEX IX_Review_Status (Status),
    INDEX IX_Review_TenantId (TenantId)
);

CREATE TABLE WatchedListings (
    WatchedListingId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    ListingId UNIQUEIDENTIFIER NOT NULL,
    Notes NVARCHAR(500),
    NotifyOnPriceChange BIT NOT NULL DEFAULT 1,
    NotifyOnRemoval BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,

    CONSTRAINT FK_Watch_Listing FOREIGN KEY (ListingId) REFERENCES Listings(ListingId),
    CONSTRAINT UQ_Watch_Listing UNIQUE (TenantId, ListingId),
    INDEX IX_Watch_TenantId (TenantId)
);

CREATE TABLE Alerts (
    AlertId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Criteria NVARCHAR(MAX) NOT NULL,        -- JSON serialized search criteria
    NotificationMethod NVARCHAR(50) NOT NULL, -- Console, Email, Webhook
    NotificationTarget NVARCHAR(500),
    IsActive BIT NOT NULL DEFAULT 1,
    LastTriggeredAt DATETIME2,
    TriggerCount INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,

    INDEX IX_Alert_TenantId (TenantId),
    INDEX IX_Alert_Active (IsActive)
);

CREATE TABLE AlertNotifications (
    NotificationId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    AlertId UNIQUEIDENTIFIER NOT NULL,
    ListingId UNIQUEIDENTIFIER NOT NULL,
    SentAt DATETIME2 NOT NULL,
    Status NVARCHAR(20) NOT NULL,           -- Sent, Failed, Pending

    CONSTRAINT FK_Notification_Alert FOREIGN KEY (AlertId) REFERENCES Alerts(AlertId),
    CONSTRAINT FK_Notification_Listing FOREIGN KEY (ListingId) REFERENCES Listings(ListingId)
);

CREATE TABLE Dealers (
    DealerId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    NormalizedName NVARCHAR(200) NOT NULL,
    City NVARCHAR(100),
    State NVARCHAR(50),
    Phone NVARCHAR(20),
    ListingCount INT NOT NULL DEFAULT 0,
    FirstSeenAt DATETIME2 NOT NULL,
    LastSeenAt DATETIME2 NOT NULL,

    INDEX IX_Dealer_NormalizedName (NormalizedName),
    INDEX IX_Dealer_TenantId (TenantId)
);

CREATE TABLE ScraperHealth (
    HealthRecordId UNIQUEIDENTIFIER PRIMARY KEY,
    SiteName NVARCHAR(50) NOT NULL,
    RecordedAt DATETIME2 NOT NULL,
    SuccessRate DECIMAL(5,2) NOT NULL,
    ListingsFound INT NOT NULL,
    ErrorCount INT NOT NULL,
    AverageResponseTime INT NOT NULL,       -- milliseconds
    LastError NVARCHAR(MAX),
    Status NVARCHAR(20) NOT NULL,           -- Healthy, Degraded, Failed

    INDEX IX_Health_Site (SiteName),
    INDEX IX_Health_RecordedAt (RecordedAt)
);

-- Schema Modifications

ALTER TABLE Listings ADD
    ImageHashes NVARCHAR(MAX) NULL,         -- JSON array of 64-bit hashes
    DealerId UNIQUEIDENTIFIER NULL,
    RelistedCount INT NOT NULL DEFAULT 0,
    PreviousListingId UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_Listing_Dealer FOREIGN KEY (DealerId) REFERENCES Dealers(DealerId);

-- New Indexes
CREATE INDEX IX_Listing_DealerId ON Listings(DealerId);
CREATE INDEX IX_Listing_RelistedCount ON Listings(RelistedCount) WHERE RelistedCount > 0;
```

---

## Implementation Sequence

### Phase 4 Implementation Timeline

```
Sprint 1: Image Analysis Infrastructure (Week 1-2)
    │
    ├── 1.1 Image Download Service
    │       ├── HTTP client for image fetching
    │       ├── Retry logic for failures
    │       └── Image format validation
    │
    ├── 1.2 Perceptual Hash Algorithm
    │       ├── Grayscale conversion
    │       ├── DCT-based hashing
    │       └── Hash comparison (Hamming)
    │
    ├── 1.3 Image Hashing Service
    │       ├── Integration with duplicate detection
    │       ├── Hash storage in Listing
    │       └── Configuration options
    │
    └── 1.4 Enhanced Duplicate Detection
            ├── Combine fuzzy + image scores
            └── Configurable image weight

Sprint 2: Near-Match Review System (Week 3-4)
    │
    ├── 2.1 Review Queue Entity
    │       ├── Review item model
    │       ├── Status tracking
    │       └── Resolution decisions
    │
    ├── 2.2 Review Service
    │       ├── Queue management
    │       ├── Resolution persistence
    │       └── Learning from decisions
    │
    ├── 2.3 Review Command
    │       ├── List pending reviews
    │       ├── Display comparison
    │       └── Accept/reject actions
    │
    └── 2.4 Relisted Detection
            ├── Match to historical
            ├── Track off-market time
            └── Price delta calculation

Sprint 3: Concurrent Scraping (Week 5-6)
    │
    ├── 3.1 Concurrent Scraping Engine
    │       ├── Parallel.ForEachAsync
    │       ├── Resource management
    │       └── Result aggregation
    │
    ├── 3.2 Concurrency Configuration
    │       ├── --concurrency option
    │       ├── Default of 3 sites
    │       └── Per-site contexts
    │
    ├── 3.3 Resource Throttling
    │       ├── CPU/memory monitoring
    │       └── Adaptive throttling
    │
    └── 3.4 Proxy Rotation
            ├── Multiple proxy support
            └── Failed proxy removal

Sprint 4: Health Monitoring (Week 7-8)
    │
    ├── 4.1 Health Metrics Collection
    │       ├── Success rate tracking
    │       ├── Response time measurement
    │       └── Error categorization
    │
    ├── 4.2 Missing Element Detection
    │       ├── Expected element checks
    │       ├── Zero result warnings
    │       └── Structure change alerts
    │
    ├── 4.3 Health Service
    │       ├── Status aggregation
    │       ├── Historical tracking
    │       └── Trend analysis
    │
    ├── 4.4 Screenshot/Debug Capture
    │       ├── Error screenshots
    │       ├── HTML source saving
    │       └── Debug artifact management
    │
    └── 4.5 Status Command Enhancement
            └── Detailed health display

Sprint 5: User Features (Week 9-10)
    │
    ├── 5.1 Watch List
    │       ├── Add/remove listings
    │       ├── Change notifications
    │       └── Watch list display
    │
    ├── 5.2 Comparison View
    │       ├── Side-by-side display
    │       ├── Difference highlighting
    │       └── Best value marking
    │
    ├── 5.3 Alert System
    │       ├── Alert creation
    │       ├── Criteria matching
    │       └── Notification dispatch
    │
    ├── 5.4 Dealer Tracking
    │       ├── Dealer entity
    │       ├── Name normalization
    │       └── Inventory linking
    │
    └── 5.5 Price History Enhancement
            └── Visual trend display

Sprint 6: Data Operations (Week 11-12)
    │
    ├── 6.1 Data Import Service
    │       ├── CSV import
    │       ├── JSON import
    │       ├── Validation
    │       └── Dry-run mode
    │
    ├── 6.2 Import Command
    │       ├── Format detection
    │       ├── Progress display
    │       └── Error reporting
    │
    ├── 6.3 Backup Service
    │       ├── Database backup
    │       ├── Timestamped files
    │       └── Retention policy
    │
    ├── 6.4 Backup/Restore Commands
    │       ├── backup command
    │       └── restore command
    │
    └── 6.5 Streamed Export
            └── Memory-efficient large exports

Sprint 7: CLI Polish (Week 13-14)
    │
    ├── 7.1 Shell Completion
    │       ├── Bash completion
    │       ├── Zsh completion
    │       └── PowerShell completion
    │
    ├── 7.2 Signal Handling
    │       ├── SIGINT (Ctrl+C)
    │       ├── Graceful shutdown
    │       └── Partial results save
    │
    ├── 7.3 Confirmation Prompts
    │       ├── Destructive operations
    │       ├── --yes bypass
    │       └── --dry-run preview
    │
    ├── 7.4 Logs Command
    │       └── Log file viewing
    │
    └── 7.5 Integration & Testing
            ├── E2E image matching tests
            ├── Concurrent scraping tests
            └── Documentation updates
```

---

## Sprint Breakdown

### Sprint 1: Image Analysis Infrastructure

#### Goals
- Implement perceptual hashing for images
- Create image download service
- Integrate image matching into duplicate detection

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S1-01 | Add ImageSharp NuGet package | None | P0 |
| S1-02 | Create IImageDownloadService interface | None | P0 |
| S1-03 | Implement ImageDownloadService | S1-02 | P0 |
| S1-04 | Create PerceptualHashCalculator | S1-01 | P0 |
| S1-05 | Implement DCT-based hashing | S1-04 | P0 |
| S1-06 | Create IImageHashingService interface | S1-04 | P0 |
| S1-07 | Implement ImageHashingService | S1-06, S1-03 | P0 |
| S1-08 | Add ImageHashes field to Listing | None | P0 |
| S1-09 | Update ListingConfiguration | S1-08 | P0 |
| S1-10 | Create Phase 4 migration | S1-08 | P0 |
| S1-11 | Integrate image matching into DuplicateDetectionService | S1-07 | P0 |
| S1-12 | Add --image-matching CLI option | S1-11 | P0 |
| S1-13 | Add image hashing unit tests | S1-05 | P0 |
| S1-14 | Add integration tests | S1-11 | P1 |

#### Key Code Artifacts

**PerceptualHashCalculator.cs** (Core/Services/ImageAnalysis/)
```csharp
namespace AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;

public class PerceptualHashCalculator
{
    private const int HashSize = 8;
    private const int DctSize = 32;

    public ulong CalculateHash(byte[] imageData)
    {
        using var image = Image.Load<Rgba32>(imageData);

        // Convert to grayscale and resize
        image.Mutate(x => x
            .Grayscale()
            .Resize(DctSize, DctSize));

        // Extract pixel values
        var pixels = new double[DctSize, DctSize];
        for (int y = 0; y < DctSize; y++)
        {
            for (int x = 0; x < DctSize; x++)
            {
                pixels[y, x] = image[x, y].R;
            }
        }

        // Apply DCT
        var dct = ApplyDCT(pixels);

        // Extract low-frequency 8x8
        var subset = new double[HashSize * HashSize];
        for (int y = 0; y < HashSize; y++)
        {
            for (int x = 0; x < HashSize; x++)
            {
                subset[y * HashSize + x] = dct[y, x];
            }
        }

        // Calculate median
        var median = subset.OrderBy(v => v).ElementAt(subset.Length / 2);

        // Generate hash
        ulong hash = 0;
        for (int i = 0; i < 64; i++)
        {
            if (subset[i] > median)
                hash |= (1UL << i);
        }

        return hash;
    }

    public int HammingDistance(ulong hash1, ulong hash2)
    {
        var xor = hash1 ^ hash2;
        return BitOperations.PopCount(xor);
    }

    public bool IsSimilar(ulong hash1, ulong hash2, int threshold = 10)
    {
        return HammingDistance(hash1, hash2) <= threshold;
    }
}
```

**ImageHashingService.cs** (Core/Services/ImageAnalysis/)
```csharp
namespace AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;

public class ImageHashingService : IImageHashingService
{
    private readonly IImageDownloadService _downloadService;
    private readonly PerceptualHashCalculator _hashCalculator;
    private readonly ILogger<ImageHashingService> _logger;

    public async Task<ImageMatchResult> FindImageMatchesAsync(
        IEnumerable<string> imageUrls,
        IEnumerable<Listing> candidates,
        CancellationToken cancellationToken = default)
    {
        // Download and hash input images
        var inputHashes = new List<ulong>();
        foreach (var url in imageUrls.Take(3))
        {
            try
            {
                var imageData = await _downloadService.DownloadAsync(url, cancellationToken);
                var hash = _hashCalculator.CalculateHash(imageData);
                inputHashes.Add(hash);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process image: {Url}", url);
            }
        }

        if (inputHashes.Count == 0)
            return ImageMatchResult.NoImages();

        // Compare against candidates
        Listing? bestMatch = null;
        int bestMatchCount = 0;

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate.ImageHashes))
                continue;

            var candidateHashes = JsonSerializer.Deserialize<ulong[]>(candidate.ImageHashes);
            var matchCount = CountMatchingImages(inputHashes, candidateHashes);

            if (matchCount > bestMatchCount)
            {
                bestMatchCount = matchCount;
                bestMatch = candidate;
            }
        }

        // Require majority match
        var threshold = (int)Math.Ceiling(inputHashes.Count / 2.0);
        if (bestMatchCount >= threshold)
        {
            return new ImageMatchResult
            {
                HasMatch = true,
                MatchedListing = bestMatch,
                MatchingImageCount = bestMatchCount,
                TotalImageCount = inputHashes.Count
            };
        }

        return ImageMatchResult.NoMatch();
    }

    private int CountMatchingImages(List<ulong> hashes1, ulong[] hashes2)
    {
        return hashes1.Count(h1 =>
            hashes2.Any(h2 => _hashCalculator.IsSimilar(h1, h2)));
    }
}
```

---

### Sprint 2: Near-Match Review System

#### Goals
- Create review queue for ambiguous matches
- Implement manual resolution workflow
- Add relisted vehicle detection

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S2-01 | Create ReviewItem entity | None | P0 |
| S2-02 | Create ReviewItemConfiguration | S2-01 | P0 |
| S2-03 | Create ResolutionDecision enum | None | P0 |
| S2-04 | Create IReviewService interface | S2-01 | P0 |
| S2-05 | Implement ReviewService | S2-04 | P0 |
| S2-06 | Create ReviewCommand | S2-05 | P0 |
| S2-07 | Implement review list subcommand | S2-06 | P0 |
| S2-08 | Implement review resolve subcommand | S2-06 | P0 |
| S2-09 | Create IRelistedDetectionService | None | P1 |
| S2-10 | Implement RelistedDetectionService | S2-09 | P1 |
| S2-11 | Add RelistedCount to Listing | None | P1 |
| S2-12 | Integrate near-match flagging | S2-05 | P0 |
| S2-13 | Add review unit tests | S2-05 | P1 |

---

### Sprint 3: Concurrent Scraping

#### Goals
- Implement parallel site scraping
- Add resource management
- Implement proxy rotation

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S3-01 | Create IConcurrentScrapingEngine | None | P0 |
| S3-02 | Implement ConcurrentScrapingEngine | S3-01 | P0 |
| S3-03 | Add --concurrency CLI option | S3-02 | P0 |
| S3-04 | Create ScrapingResourceManager | None | P0 |
| S3-05 | Implement CPU/memory monitoring | S3-04 | P1 |
| S3-06 | Implement adaptive throttling | S3-05 | P2 |
| S3-07 | Create ProxyRotationService | None | P2 |
| S3-08 | Add proxy file support | S3-07 | P2 |
| S3-09 | Implement failed proxy removal | S3-07 | P2 |
| S3-10 | Add concurrent scraping tests | S3-02 | P1 |

---

### Sprint 4: Health Monitoring

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S4-01 | Create ScraperHealth entity | None | P1 |
| S4-02 | Create IScraperHealthService | S4-01 | P1 |
| S4-03 | Implement ScraperHealthService | S4-02 | P1 |
| S4-04 | Add success rate tracking | S4-03 | P1 |
| S4-05 | Add response time measurement | S4-03 | P1 |
| S4-06 | Implement element validation | None | P1 |
| S4-07 | Add zero-result warnings | S4-06 | P1 |
| S4-08 | Add screenshot on error | None | P2 |
| S4-09 | Add HTML source saving | None | P2 |
| S4-10 | Update status command | S4-03 | P1 |

---

### Sprint 5: User Features

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S5-01 | Create WatchedListing entity | None | P1 |
| S5-02 | Create IWatchListService | S5-01 | P1 |
| S5-03 | Implement WatchListService | S5-02 | P1 |
| S5-04 | Create WatchCommand | S5-03 | P1 |
| S5-05 | Create CompareCommand | None | P1 |
| S5-06 | Implement side-by-side display | S5-05 | P1 |
| S5-07 | Create Alert entity | None | P1 |
| S5-08 | Create IAlertService | S5-07 | P1 |
| S5-09 | Implement AlertService | S5-08 | P1 |
| S5-10 | Create AlertCommand | S5-09 | P1 |
| S5-11 | Implement notification dispatch | S5-09 | P1 |
| S5-12 | Create Dealer entity | None | P1 |
| S5-13 | Implement dealer tracking | S5-12 | P1 |

---

### Sprint 6: Data Operations

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S6-01 | Create IDataImportService | None | P1 |
| S6-02 | Implement CsvImporter | S6-01 | P1 |
| S6-03 | Implement JsonImporter | S6-01 | P1 |
| S6-04 | Add import validation | S6-02, S6-03 | P1 |
| S6-05 | Create ImportCommand | S6-01 | P1 |
| S6-06 | Add dry-run mode | S6-05 | P1 |
| S6-07 | Create IBackupService | None | P1 |
| S6-08 | Implement BackupService | S6-07 | P1 |
| S6-09 | Create BackupCommand | S6-08 | P1 |
| S6-10 | Create RestoreCommand | S6-08 | P1 |
| S6-11 | Add retention policy | S6-08 | P2 |
| S6-12 | Implement streamed export | None | P2 |

---

### Sprint 7: CLI Polish

#### Tasks

| ID | Task | Dependencies | Priority |
|----|------|--------------|----------|
| S7-01 | Create completion command | None | P2 |
| S7-02 | Implement Bash completion | S7-01 | P2 |
| S7-03 | Implement Zsh completion | S7-01 | P2 |
| S7-04 | Implement PowerShell completion | S7-01 | P2 |
| S7-05 | Create GracefulShutdownHandler | None | P1 |
| S7-06 | Implement SIGINT handling | S7-05 | P1 |
| S7-07 | Implement partial results save | S7-06 | P1 |
| S7-08 | Add confirmation prompts | None | P2 |
| S7-09 | Add --yes flag | S7-08 | P2 |
| S7-10 | Add --dry-run flag | None | P2 |
| S7-11 | Create LogsCommand | None | P2 |
| S7-12 | E2E testing | All | P0 |
| S7-13 | Documentation | All | P1 |

---

## Technical Specifications

### CLI Commands Reference (Phase 4)

```bash
# Review queue
car-search review list
car-search review list --status pending
car-search review resolve <review-id> --same-vehicle
car-search review resolve <review-id> --different-vehicle
car-search review dismiss <review-id>

# Watch list
car-search watch add <listing-id> --notes "Great deal"
car-search watch remove <listing-id>
car-search watch list

# Comparison
car-search compare <id1> <id2> [<id3>...]

# Alerts
car-search alert create "cheap-camry" -m Toyota --model Camry --price-max 15000
car-search alert list
car-search alert delete "cheap-camry"
car-search alert enable <alert-id>
car-search alert disable <alert-id>

# Import
car-search import listings.csv
car-search import listings.json --format json
car-search import listings.csv --dry-run

# Backup/Restore
car-search backup
car-search backup --output /path/to/backup.db
car-search restore /path/to/backup.db

# Concurrent scraping
car-search scrape --concurrency 5

# Debug capture
car-search scrape --screenshot-on-error
car-search scrape --save-html

# Shell completion
car-search completion bash
car-search completion zsh > ~/.zsh/completions/_car-search
car-search completion powershell

# Logs
car-search logs
car-search logs --tail 100

# Confirmation bypass
car-search config reset --yes
car-search import large-file.csv --dry-run
```

---

## Risk Mitigation

### Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Image hashing accuracy | Medium | Medium | Multiple image comparison, majority rule |
| Concurrent scraping resource exhaustion | Medium | High | Adaptive throttling, resource monitoring |
| Review queue backlog | Medium | Low | Configurable threshold, auto-aging |
| Backup file corruption | Low | High | Checksum validation, multiple backups |
| Signal handling data loss | Medium | Medium | Transaction-safe partial saves |

### Mitigation Strategies

1. **Image Matching**: Use established pHash algorithm; validate against known car image datasets
2. **Concurrent Performance**: Start with conservative defaults (3); monitor and adjust
3. **Review Queue**: Auto-dismiss old items (>30 days); batch resolution tools
4. **Data Safety**: Always backup before restore; SHA256 checksum on backup files

---

## Definition of Done

### Feature Level
- [ ] All acceptance criteria from specs met
- [ ] Image matching tested with diverse car images
- [ ] Concurrent scraping stress tested
- [ ] Review workflow end-to-end tested

### Sprint Level
- [ ] All P0 tasks completed
- [ ] All P1 tasks completed or deferred
- [ ] Performance benchmarks met (3x faster with concurrency)
- [ ] Health monitoring operational

### Phase Level
- [ ] All sprints completed
- [ ] Image-based detection integrated
- [ ] User features (watch/compare/alerts) functional
- [ ] Data operations (import/backup) reliable
- [ ] CLI production-ready

---

## Diagrams

### Architecture Diagrams
- [Phase 4 Architecture](./diagrams/phase-4-architecture.png)
- [Image Matching Flow](./diagrams/image-matching-flow.png)
- [Concurrent Scraping](./diagrams/concurrent-scraping.png)
- [Implementation Timeline](./diagrams/implementation-timeline.drawio.svg)

### Source Files
- [phase-4-architecture.puml](./diagrams/phase-4-architecture.puml)
- [image-matching-flow.puml](./diagrams/image-matching-flow.puml)
- [concurrent-scraping.puml](./diagrams/concurrent-scraping.puml)
- [implementation-timeline.drawio](./diagrams/implementation-timeline.drawio)

---

## Appendix

### References
- [Phase 1 Roadmap](../phase-1/README.md)
- [Phase 2 Roadmap](../phase-2/README.md)
- [Phase 3 Roadmap](../phase-3/README.md)
- [Duplicate Detection Specs](../../specs/duplicate-detection/duplicate-detection.specs.md)
- [Web Scraping Specs](../../specs/web-scraping/web-scraping.specs.md)
- [CLI Interface Specs](../../specs/cli-interface/cli-interface.specs.md)
- [Reporting Specs](../../specs/reporting/reporting.specs.md)
- [Data Persistence Specs](../../specs/data-persistence/data-persistence.specs.md)

### Changelog
| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-08 | Claude | Initial roadmap creation |
