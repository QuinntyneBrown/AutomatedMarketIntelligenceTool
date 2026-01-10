# Code Cleanup Roadmap

**Created:** January 10, 2026
**Based on:** `docs/code-review-analysis.md`
**Status:** Planning
**Branch:** `claude/code-cleanup-roadmap-y9tXW`

---

## Overview

This roadmap provides actionable tasks for cleaning up the AutomatedMarketIntelligenceTool codebase by addressing:
- **50+ duplicated code instances**
- **22+ unused code elements**
- **69+ files without requirement traceability**

Tasks are organized by priority and include detailed steps with file locations.

---

## Table of Contents

1. [Phase 1: Duplicated Code Removal](#phase-1-duplicated-code-removal)
2. [Phase 2: Unused Code Removal](#phase-2-unused-code-removal)
3. [Phase 3: Code Without Requirements](#phase-3-code-without-requirements)
4. [Phase 4: Verification & Testing](#phase-4-verification--testing)

---

## Phase 1: Duplicated Code Removal

### 1.1 Create Shared StringUtilities Class

**Priority:** High
**Estimated Impact:** Removes 4 duplicate methods

- [ ] **1.1.1** Create new file `src/AutomatedMarketIntelligenceTool.Core/Utilities/StringUtilities.cs`
- [ ] **1.1.2** Implement `EscapeCsvValue()` method in StringUtilities
  - Source reference: `src/AutomatedMarketIntelligenceTool.Cli/Formatters/CsvFormatter.cs:41`
- [ ] **1.1.3** Implement `SanitizeFileName()` method in StringUtilities
  - Source reference: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Health/DebugCaptureService.cs:110`
- [ ] **1.1.4** Update `CsvFormatter.cs` to use `StringUtilities.EscapeCsvValue()`
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Formatters/CsvFormatter.cs`
  - Remove local `EscapeCsvValue()` method at line 41
- [ ] **1.1.5** Update `ExportCommand.cs` to use `StringUtilities.EscapeCsvValue()`
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/ExportCommand.cs`
  - Remove local `EscapeCsv()` method at line 323
- [ ] **1.1.6** Update `DebugCaptureService.cs` to use `StringUtilities.SanitizeFileName()`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Health/DebugCaptureService.cs`
  - Remove local `SanitizeFileName()` method at line 110
- [ ] **1.1.7** Update `ScheduledReport.cs` to use `StringUtilities.SanitizeFileName()`
  - File: `src/AutomatedMarketIntelligenceTool.Core/Models/ScheduledReportAggregate/ScheduledReport.cs`
  - Remove local `SanitizeFilename()` method at line 440
- [ ] **1.1.8** Verify all usages compile and tests pass

---

### 1.2 Create BaseScraper Abstract Class with Shared Helpers

**Priority:** High
**Estimated Impact:** Removes 46+ duplicate methods across 10 scrapers

#### 1.2.1 Create Base Class

- [ ] **1.2.1.1** Create `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/BaseScraper.cs`
- [ ] **1.2.1.2** Add abstract class structure with common interface implementation
- [ ] **1.2.1.3** Implement shared `ParsePrice()` method
  ```csharp
  protected static decimal ParsePrice(string priceText)
  {
      var cleanPrice = priceText.Replace("$", string.Empty)
          .Replace(",", string.Empty)
          .Trim();
      return decimal.TryParse(cleanPrice, out var price) ? price : 0;
  }
  ```
- [ ] **1.2.1.4** Implement shared `ParseMileage()` method
- [ ] **1.2.1.5** Implement shared `ParseLocation()` method
- [ ] **1.2.1.6** Implement shared `ParseTitle()` method
- [ ] **1.2.1.7** Implement shared `ExtractExternalId()` method (virtual for override)

#### 1.2.2 Update Auto123Scraper

- [ ] **1.2.2.1** Update `Auto123Scraper.cs` to inherit from `BaseScraper`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/Auto123Scraper.cs`
- [ ] **1.2.2.2** Remove `ExtractExternalId()` at line 190 (use base)
- [ ] **1.2.2.3** Remove `ParsePrice()` at line 223 (use base)
- [ ] **1.2.2.4** Remove `ParseMileage()` at line 238 (use base)
- [ ] **1.2.2.5** Remove `ParseLocation()` at line 254 (use base)
- [ ] **1.2.2.6** Remove `ParseTitle()` at line 265 (use base)

#### 1.2.3 Update AutotraderScraper

- [ ] **1.2.3.1** Update `AutotraderScraper.cs` to inherit from `BaseScraper`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/AutotraderScraper.cs`
- [ ] **1.2.3.2** Remove `ExtractExternalId()` at line 230 (use base)
- [ ] **1.2.3.3** Remove `ParsePrice()` at line 257 (use base)
- [ ] **1.2.3.4** Remove `ParseMileage()` at line 271 (use base)
- [ ] **1.2.3.5** Remove `ParseLocation()` at line 289 (use base)
- [ ] **1.2.3.6** Remove `ParseTitle()` at line 300 (use base)

#### 1.2.4 Update CarFaxScraper

- [ ] **1.2.4.1** Update `CarFaxScraper.cs` to inherit from `BaseScraper`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/CarFaxScraper.cs`
- [ ] **1.2.4.2** Remove `ExtractExternalId()` at line 198 (use base)
- [ ] **1.2.4.3** Remove `ParsePrice()` at line 231 (use base)
- [ ] **1.2.4.4** Remove `ParseMileage()` at line 246 (use base)
- [ ] **1.2.4.5** Remove `ParseLocation()` at line 262 (use base)
- [ ] **1.2.4.6** Remove `ParseTitle()` at line 273 (use base)

#### 1.2.5 Update CarGurusScraper

- [ ] **1.2.5.1** Update `CarGurusScraper.cs` to inherit from `BaseScraper`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/CarGurusScraper.cs`
- [ ] **1.2.5.2** Remove `ExtractExternalId()` at line 216 (use base)
- [ ] **1.2.5.3** Remove `ParsePrice()` at line 229 (use base)
- [ ] **1.2.5.4** Remove `ParseMileage()` at line 247 (use base)

#### 1.2.6 Update CarMaxScraper

- [ ] **1.2.6.1** Update `CarMaxScraper.cs` to inherit from `BaseScraper`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/CarMaxScraper.cs`
- [ ] **1.2.6.2** Remove `ExtractExternalId()` at line 194 (use base)
- [ ] **1.2.6.3** Remove `ParsePrice()` at line 227 (use base)
- [ ] **1.2.6.4** Remove `ParseMileage()` at line 242 (use base)
- [ ] **1.2.6.5** Remove `ParseLocation()` at line 258 (use base)
- [ ] **1.2.6.6** Remove `ParseTitle()` at line 269 (use base)

#### 1.2.7 Update CarvanaScraper

- [ ] **1.2.7.1** Update `CarvanaScraper.cs` to inherit from `BaseScraper`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/CarvanaScraper.cs`
- [ ] **1.2.7.2** Remove `ExtractExternalId()` at line 187 (use base)
- [ ] **1.2.7.3** Remove `ParsePrice()` at line 220 (use base)
- [ ] **1.2.7.4** Remove `ParseMileage()` at line 235 (use base)
- [ ] **1.2.7.5** Remove `ParseTitle()` at line 251 (use base)

#### 1.2.8 Update ClutchScraper

- [ ] **1.2.8.1** Update `ClutchScraper.cs` to inherit from `BaseScraper`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/ClutchScraper.cs`
- [ ] **1.2.8.2** Remove `ExtractExternalId()` at line 276 (use base)
- [ ] **1.2.8.3** Remove `ParsePrice()` at line 309 (use base)
- [ ] **1.2.8.4** Remove `ParseMileage()` at line 324 (use base)
- [ ] **1.2.8.5** Remove `ParseLocation()` at line 340 (use base)
- [ ] **1.2.8.6** Remove `ParseTitle()` at line 351 (use base)

#### 1.2.9 Update KijijiScraper

- [ ] **1.2.9.1** Update `KijijiScraper.cs` to inherit from `BaseScraper`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/KijijiScraper.cs`
- [ ] **1.2.9.2** Remove `ExtractExternalId()` at line 386 (use base)
- [ ] **1.2.9.3** Remove `ParsePrice()` at line 402 (use base)
- [ ] **1.2.9.4** Remove `ParseMileage()` at line 427 (use base)
- [ ] **1.2.9.5** Remove `ParseLocation()` at line 448 (use base)
- [ ] **1.2.9.6** Remove `ParseTitle()` at line 465 (use base)

#### 1.2.10 Update TrueCarScraper

- [ ] **1.2.10.1** Update `TrueCarScraper.cs` to inherit from `BaseScraper`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/TrueCarScraper.cs`
- [ ] **1.2.10.2** Remove `ExtractExternalId()` at line 191 (use base)
- [ ] **1.2.10.3** Remove `ParsePrice()` at line 224 (use base)
- [ ] **1.2.10.4** Remove `ParseMileage()` at line 239 (use base)
- [ ] **1.2.10.5** Remove `ParseLocation()` at line 255 (use base)
- [ ] **1.2.10.6** Remove `ParseTitle()` at line 266 (use base)

#### 1.2.11 Update VroomScraper

- [ ] **1.2.11.1** Update `VroomScraper.cs` to inherit from `BaseScraper`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/VroomScraper.cs`
- [ ] **1.2.11.2** Remove `ExtractExternalId()` at line 176 (use base)
- [ ] **1.2.11.3** Remove `ParsePrice()` at line 209 (use base)
- [ ] **1.2.11.4** Remove `ParseMileage()` at line 224 (use base)
- [ ] **1.2.11.5** Remove `ParseTitle()` at line 240 (use base)

#### 1.2.12 Verification

- [ ] **1.2.12.1** Run all scraper unit tests
- [ ] **1.2.12.2** Verify compilation with no errors
- [ ] **1.2.12.3** Run integration tests for at least 2 scrapers

---

## Phase 2: Unused Code Removal

### 2.1 Unregistered CLI Commands

**Priority:** High
**Decision Required:** Register or Delete

#### Option A: Delete Unregistered Commands

- [ ] **2.1.1** Delete `AlertCommand.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/AlertCommand.cs`
  - Reason: Not registered in Program.cs, not required for MVP
- [ ] **2.1.2** Delete `CompareCommand.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/CompareCommand.cs`
  - Reason: No requirement ID found, not registered
- [ ] **2.1.3** Delete `DashboardCommand.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/DashboardCommand.cs`
  - Reason: Phase 5 feature, not registered
- [ ] **2.1.4** Delete `ReviewCommand.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/ReviewCommand.cs`
  - Reason: Phase 4 feature, not registered
- [ ] **2.1.5** Delete `WatchCommand.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/WatchCommand.cs`
  - Reason: Phase 3-4 feature, not registered

#### Option B: Register Commands (Alternative)

- [ ] **2.1.B.1** Add AlertCommand registration to `Program.cs` at line ~280
- [ ] **2.1.B.2** Add CompareCommand registration to `Program.cs`
- [ ] **2.1.B.3** Add DashboardCommand registration to `Program.cs`
- [ ] **2.1.B.4** Add ReviewCommand registration to `Program.cs`
- [ ] **2.1.B.5** Add WatchCommand registration to `Program.cs`

---

### 2.2 Unregistered Core Services

**Priority:** High
**Decision Required:** Register or Delete

#### Option A: Delete Unregistered Services

- [ ] **2.2.1** Delete `NewListingDetectionService.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Core/Services/NewListingDetectionService.cs`
  - Interface: `INewListingDetectionService`
- [ ] **2.2.2** Delete `PriceChangeDetectionService.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Core/Services/PriceChangeDetectionService.cs`
  - Interface: `IPriceChangeDetectionService`
- [ ] **2.2.3** Delete `ListingDeactivationService.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Core/Services/ListingDeactivationService.cs`
  - Interface: `IListingDeactivationService`
- [ ] **2.2.4** Delete `DealerTrackingService.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Core/Services/DealerTrackingService.cs`
  - Interface: `IDealerTrackingService`
- [ ] **2.2.5** Delete `VehicleLinkingService.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Core/Services/VehicleLinkingService.cs`
  - Interface: `IVehicleLinkingService`
- [ ] **2.2.6** Delete corresponding interface files for each service

---

### 2.3 Unregistered Infrastructure Services

**Priority:** High
**Decision Required:** Register or Delete

#### Option A: Delete Unregistered Services

- [ ] **2.3.1** Delete `ResponseCacheService.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Cache/ResponseCacheService.cs`
  - Reason: Phase 5 feature (REQ-WS-014)
- [ ] **2.3.2** Delete `DebugCaptureService.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Debug/DebugCaptureService.cs`
  - Reason: Not registered, debugging feature
- [ ] **2.3.3** Delete `HeaderConfigurationService.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Headers/HeaderConfigurationService.cs`
  - Reason: Phase 5 feature (REQ-WS-013)
- [ ] **2.3.4** Delete `ProxyRotationService.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Proxy/ProxyRotationService.cs`
  - Reason: Phase 3 feature (REQ-WS-007)
- [ ] **2.3.5** Delete `ProxyService.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Proxy/ProxyService.cs`
  - Reason: Phase 3 feature (REQ-WS-007)
- [ ] **2.3.6** Delete `BatchDeduplicationService.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Deduplication/BatchDeduplicationService.cs`
  - Reason: Phase 5 feature

---

### 2.4 Fix Missing Service Registration

**Priority:** Critical (Blocking)

- [ ] **2.4.1** Create `IScraperHealthService` interface if missing
- [ ] **2.4.2** Create `ScraperHealthService` implementation if missing
- [ ] **2.4.3** Register in `Program.cs`:
  ```csharp
  services.AddScoped<IScraperHealthService, ScraperHealthService>();
  ```
- [ ] **2.4.4** Verify `DashboardCommand.cs` compiles (line 15, 21)
- [ ] **2.4.5** Verify `StatusCommand.cs` compiles (line 15, 21)

---

### 2.5 Remove or Implement Unimplemented Features

**Priority:** Medium
**File:** `src/AutomatedMarketIntelligenceTool.Core/Services/AlertService.cs`

#### Option A: Remove Unimplemented Notification Methods

- [ ] **2.5.1** Remove `Email` case from AlertService (lines 167-171)
- [ ] **2.5.2** Remove `Webhook` case from AlertService (lines 173-177)
- [ ] **2.5.3** Remove `Email` from `NotificationMethod` enum
  - File: `src/AutomatedMarketIntelligenceTool.Core/Models/AlertAggregate/NotificationMethod.cs`
- [ ] **2.5.4** Remove `Webhook` from `NotificationMethod` enum
- [ ] **2.5.5** Search codebase for any references to removed enum values and update

#### Option B: Implement Notification Methods (Alternative)

- [ ] **2.5.B.1** Implement email notification using SMTP
- [ ] **2.5.B.2** Implement webhook notification using HttpClient
- [ ] **2.5.B.3** Add configuration for email/webhook settings
- [ ] **2.5.B.4** Add tests for notification methods

---

### 2.6 Remove Registered But Unused Service

**Priority:** Low

- [ ] **2.6.1** Remove `IRelistedDetectionService` registration from `Program.cs:125`
- [ ] **2.6.2** Delete `RelistedDetectionService.cs` if not needed
- [ ] **2.6.3** Delete `IRelistedDetectionService.cs` interface

---

## Phase 3: Code Without Requirements

### 3.1 Web Scrapers Beyond MVP Scope

**Priority:** Medium
**MVP Requirement:** Only Autotrader.ca and Kijiji.ca (REQ-WS-001)
**Decision Required:** Keep for future phases or Delete

#### Option A: Delete Non-MVP Scrapers

- [ ] **3.1.1** Delete `Auto123Scraper.cs` (Phase 5)
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/Auto123Scraper.cs`
- [ ] **3.1.2** Delete `CarFaxScraper.cs` (No requirement)
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/CarFaxScraper.cs`
- [ ] **3.1.3** Delete `CarGurusScraper.cs` (No requirement)
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/CarGurusScraper.cs`
- [ ] **3.1.4** Delete `CarMaxScraper.cs` (US-based, spec requires Canadian)
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/CarMaxScraper.cs`
- [ ] **3.1.5** Delete `CarvanaScraper.cs` (US-based, spec requires Canadian)
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/CarvanaScraper.cs`
- [ ] **3.1.6** Delete `ClutchScraper.cs` (No requirement)
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/ClutchScraper.cs`
- [ ] **3.1.7** Delete `TrueCarScraper.cs` (No requirement)
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/TrueCarScraper.cs`
- [ ] **3.1.8** Delete `VroomScraper.cs` (No requirement)
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/VroomScraper.cs`
- [ ] **3.1.9** Remove scraper registrations from DI container
- [ ] **3.1.10** Remove any tests for deleted scrapers

#### Option B: Keep and Document (Alternative)

- [ ] **3.1.B.1** Add feature flags to disable non-MVP scrapers
- [ ] **3.1.B.2** Document each scraper with requirement traceability
- [ ] **3.1.B.3** Move non-MVP scrapers to separate assembly for lazy loading

---

### 3.2 Database Aggregates Beyond MVP Scope

**Priority:** Medium
**MVP Requirement:** ListingAggregate, SearchSessionAggregate (optional)
**Decision Required:** Keep for future phases or Delete

#### Option A: Delete Non-MVP Aggregates

- [ ] **3.2.1** Delete `AlertAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/AlertAggregate/`
- [ ] **3.2.2** Delete `CacheAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/CacheAggregate/`
- [ ] **3.2.3** Delete `CustomMarketAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/CustomMarketAggregate/`
- [ ] **3.2.4** Delete `DealerAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/DealerAggregate/`
- [ ] **3.2.5** Delete `DeduplicationAuditAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/DeduplicationAuditAggregate/`
- [ ] **3.2.6** Delete `DeduplicationConfigAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/DeduplicationConfigAggregate/`
- [ ] **3.2.7** Delete `PriceHistoryAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/PriceHistoryAggregate/`
- [ ] **3.2.8** Delete `ReportAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/ReportAggregate/`
- [ ] **3.2.9** Delete `ResourceThrottleAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/ResourceThrottleAggregate/`
- [ ] **3.2.10** Delete `ReviewQueueAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/ReviewQueueAggregate/`
- [ ] **3.2.11** Delete `ScheduledReportAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/ScheduledReportAggregate/`
- [ ] **3.2.12** Delete `ScraperHealthAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/ScraperHealthAggregate/`
- [ ] **3.2.13** Delete `VehicleAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/VehicleAggregate/`
- [ ] **3.2.14** Delete `WatchListAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/WatchListAggregate/`
- [ ] **3.2.15** Delete `ScrapedListingAggregate/` directory
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Models/ScrapedListingAggregate/`
- [ ] **3.2.16** Update DbContext to remove deleted aggregate DbSets
- [ ] **3.2.17** Remove any migrations referencing deleted aggregates

---

### 3.3 CLI Commands Beyond MVP Scope

**Priority:** Medium
**MVP Requirement:** search, scrape (Phase 1)

#### Commands to Review/Remove

- [ ] **3.3.1** Review `show` command - No requirement found
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/ShowCommand.cs`
  - Decision: Delete or Document
- [ ] **3.3.2** Review `stats` command - Not explicitly required
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/StatsCommand.cs`
  - Decision: Delete or Document
- [ ] **3.3.3** Review `import` command - Phase 3+
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/ImportCommand.cs`
  - Decision: Delete or Keep for Phase 3
- [ ] **3.3.4** Review `backup` command - Phase 3+
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/BackupCommand.cs`
  - Decision: Delete or Keep for Phase 3
- [ ] **3.3.5** Review `restore` command - Phase 3+
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/RestoreCommand.cs`
  - Decision: Delete or Keep for Phase 3
- [ ] **3.3.6** Review `report` command - Phase 5
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/ReportCommand.cs`
  - Decision: Delete or Keep for Phase 5
- [ ] **3.3.7** Review `audit` command - Phase 5
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/AuditCommand.cs`
  - Decision: Delete or Keep for Phase 5
- [ ] **3.3.8** Review `market` command - Phase 5
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/MarketCommand.cs`
  - Decision: Delete or Keep for Phase 5
- [ ] **3.3.9** Review `schedule` command - Phase 4
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/ScheduleCommand.cs`
  - Decision: Delete or Keep for Phase 4
- [ ] **3.3.10** Review `throttle` command - Phase 5
  - File: `src/AutomatedMarketIntelligenceTool.Cli/Commands/ThrottleCommand.cs`
  - Decision: Delete or Keep for Phase 5

---

### 3.4 Services Beyond MVP Scope

**Priority:** Medium
**MVP Requirement:** SearchService, DuplicateDetectionService, basic rate limiting

#### Image Analysis Services (Phase 4)

- [ ] **3.4.1** Delete or defer `ImageAnalysis/` directory (3 files)
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Services/ImageAnalysis/`
  - Requirement: REQ-DD-004 (Phase 4)

#### Fuzzy Matching Services (Phase 3)

- [ ] **3.4.2** Review `FuzzyMatching/` services (6 files)
  - Path: `src/AutomatedMarketIntelligenceTool.Core/Services/FuzzyMatching/`
  - Keep: `GeoDistanceCalculator`, `NumericProximityCalculator`, `LevenshteinCalculator` (design pattern)
  - Review others for removal

#### Reporting Services (Phase 2-5)

- [ ] **3.4.3** Delete or defer reporting services (5+ files)
  - Review all files in reporting-related directories

#### Dashboard Services (Phase 5)

- [ ] **3.4.4** Delete dashboard services (2 files)

#### Scheduling Services (Phase 3-4)

- [ ] **3.4.5** Delete scheduling services (2 files)

#### Custom Market Services (Phase 5)

- [ ] **3.4.6** Delete custom market services (2 files)

#### Resource Throttle Services (Phase 5)

- [ ] **3.4.7** Delete resource throttle services (1 file)

#### Review Services (Phase 4)

- [ ] **3.4.8** Delete review services (1 file)

---

### 3.5 Infrastructure Files Without Requirements

**Priority:** Low

- [ ] **3.5.1** Delete `ConcurrentScrapingEngine.cs`
  - File: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scraping/ConcurrentScrapingEngine.cs`
  - Reason: REQ-WS-011 (Phase 4)

---

## Phase 4: Verification & Testing

### 4.1 Build Verification

- [ ] **4.1.1** Run `dotnet clean` on solution
- [ ] **4.1.2** Run `dotnet build` and fix any compilation errors
- [ ] **4.1.3** Resolve any missing reference errors
- [ ] **4.1.4** Update any broken using statements

### 4.2 Test Verification

- [ ] **4.2.1** Run all unit tests
- [ ] **4.2.2** Fix any failing tests due to removed code
- [ ] **4.2.3** Delete tests for removed functionality
- [ ] **4.2.4** Run integration tests

### 4.3 Database Verification

- [ ] **4.3.1** Check EF Core migrations for issues
- [ ] **4.3.2** Create new migration if model changes occurred
- [ ] **4.3.3** Test database operations with reduced model

### 4.4 Documentation Updates

- [ ] **4.4.1** Update README.md to reflect removed features
- [ ] **4.4.2** Update any API documentation
- [ ] **4.4.3** Archive removed code documentation (if needed for future phases)

---

## Summary Metrics

| Category | Items to Address | Priority |
|----------|------------------|----------|
| Duplicated Code | 50+ instances | High |
| - StringUtilities | 4 methods | High |
| - BaseScraper | 46+ methods | High |
| Unused Code | 22+ elements | High |
| - CLI Commands | 5 commands | High |
| - Core Services | 5 services | High |
| - Infrastructure Services | 6 services | High |
| - Missing Registration | 1 service (blocking) | Critical |
| Code Without Requirements | 69+ files | Medium |
| - Extra Scrapers | 8 scrapers | Medium |
| - Extra Aggregates | 15 aggregates | Medium |
| - Extra CLI Commands | ~10 commands | Medium |
| - Extra Services | 30+ services | Medium |

---

## Decision Log

| Date | Decision | Made By | Rationale |
|------|----------|---------|-----------|
| | | | |

*Use this table to track decisions made during cleanup.*

---

## Notes

1. **Before deleting any code**, consider:
   - Is it needed for an upcoming phase?
   - Does it have value for future development?
   - Can it be moved to a separate branch/archive?

2. **Recommended Approach**:
   - Start with Phase 1 (duplicated code) - lowest risk
   - Phase 2.4 (missing registration) should be done immediately if blocking
   - Phase 3 requires business decisions on scope

3. **Git Strategy**:
   - Create feature branches for each phase
   - Use descriptive commit messages
   - Consider squash merging for cleaner history

---

**End of Roadmap**
