# Code Review Analysis Report


 


**Date:** January 9, 2026


**Reviewer:** Automated Code Analysis


**Branch:** `claude/code-review-cleanup-LNT83`


**Status:** Comprehensive Review Complete


 


---


 


## Executive Summary


 


This document provides a comprehensive analysis of the AutomatedMarketIntelligenceTool codebase, identifying:


- **50+ instances of duplicated code** across scrapers and utilities


- **40+ unused or unregistered code elements** including 5 CLI commands and 11 services


- **69+ files implementing features beyond documented requirements**


 


---


 


## Table of Contents


 


1. [Duplicated Code Analysis](#1-duplicated-code-analysis)


2. [Unused Code Analysis](#2-unused-code-analysis)


3. [Code Without Requirement Traceability](#3-code-without-requirement-traceability)


4. [Summary Statistics](#4-summary-statistics)


5. [Recommendations](#5-recommendations)


 


---


 


## 1. Duplicated Code Analysis


 


### 1.1 CSV Escaping Methods (2 Duplicates)


 


Identical CSV escaping logic exists in two locations:


 


| Location | File Path | Line | Method Name |


|----------|-----------|------|-------------|


| CLI Formatter | `src/AutomatedMarketIntelligenceTool.Cli/Formatters/CsvFormatter.cs` | 41 | `EscapeCsvValue()` |


| CLI Command | `src/AutomatedMarketIntelligenceTool.Cli/Commands/ExportCommand.cs` | 323 | `EscapeCsv()` |


 


**Duplicate Logic:**


```csharp


if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))


{


    return $"\"{value.Replace("\"", "\"\"")}\"";


}


return value;


```


 


**Impact:** Maintenance burden; changes to CSV escaping must be made in both locations.


 


---


 


### 1.2 File Name Sanitization Methods (2 Duplicates)


 


| Location | File Path | Line | Method Name |


|----------|-----------|------|-------------|


| Infrastructure | `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Health/DebugCaptureService.cs` | 110 | `SanitizeFileName()` |


| Core Model | `src/AutomatedMarketIntelligenceTool.Core/Models/ScheduledReportAggregate/ScheduledReport.cs` | 440 | `SanitizeFilename()` |


 


**Duplicate Logic:**


```csharp


private static string SanitizeFileName(string name)


{


    var invalid = Path.GetInvalidFileNameChars();


    return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));


}


```


 


---


 


### 1.3 Scraper Helper Methods (40+ Duplicates)


 


All 10 web scrapers implement nearly identical helper methods. This represents significant code duplication.


 


#### 1.3.1 ExtractExternalId (10 Duplicates)


 


| Scraper Class | File Path | Line |


|---------------|-----------|------|


| Auto123Scraper | `src/.../Infrastructure/Services/Scrapers/Auto123Scraper.cs` | 190 |


| AutotraderScraper | `src/.../Infrastructure/Services/Scrapers/AutotraderScraper.cs` | 230 |


| CarFaxScraper | `src/.../Infrastructure/Services/Scrapers/CarFaxScraper.cs` | 198 |


| CarGurusScraper | `src/.../Infrastructure/Services/Scrapers/CarGurusScraper.cs` | 216 |


| CarMaxScraper | `src/.../Infrastructure/Services/Scrapers/CarMaxScraper.cs` | 194 |


| CarvanaScraper | `src/.../Infrastructure/Services/Scrapers/CarvanaScraper.cs` | 187 |


| ClutchScraper | `src/.../Infrastructure/Services/Scrapers/ClutchScraper.cs` | 276 |


| KijijiScraper | `src/.../Infrastructure/Services/Scrapers/KijijiScraper.cs` | 386 |


| TrueCarScraper | `src/.../Infrastructure/Services/Scrapers/TrueCarScraper.cs` | 191 |


| VroomScraper | `src/.../Infrastructure/Services/Scrapers/VroomScraper.cs` | 176 |


 


#### 1.3.2 ParsePrice (10 Duplicates)


 


| Scraper Class | File Path | Line |


|---------------|-----------|------|


| Auto123Scraper | `src/.../Infrastructure/Services/Scrapers/Auto123Scraper.cs` | 223 |


| AutotraderScraper | `src/.../Infrastructure/Services/Scrapers/AutotraderScraper.cs` | 257 |


| CarFaxScraper | `src/.../Infrastructure/Services/Scrapers/CarFaxScraper.cs` | 231 |


| CarGurusScraper | `src/.../Infrastructure/Services/Scrapers/CarGurusScraper.cs` | 229 |


| CarMaxScraper | `src/.../Infrastructure/Services/Scrapers/CarMaxScraper.cs` | 227 |


| CarvanaScraper | `src/.../Infrastructure/Services/Scrapers/CarvanaScraper.cs` | 220 |


| ClutchScraper | `src/.../Infrastructure/Services/Scrapers/ClutchScraper.cs` | 309 |


| KijijiScraper | `src/.../Infrastructure/Services/Scrapers/KijijiScraper.cs` | 402 |


| TrueCarScraper | `src/.../Infrastructure/Services/Scrapers/TrueCarScraper.cs` | 224 |


| VroomScraper | `src/.../Infrastructure/Services/Scrapers/VroomScraper.cs` | 209 |


 


**Common Pattern:**


```csharp


private static decimal ParsePrice(string priceText)


{


    var cleanPrice = priceText.Replace("$", string.Empty)


        .Replace(",", string.Empty)


        .Replace("[CURRENCY]", string.Empty, StringComparison.OrdinalIgnoreCase)


        .Trim();


    return decimal.TryParse(cleanPrice, out var price) ? price : 0;


}


```


 


#### 1.3.3 ParseMileage (10 Duplicates)


 


| Scraper Class | File Path | Line |


|---------------|-----------|------|


| Auto123Scraper | `src/.../Infrastructure/Services/Scrapers/Auto123Scraper.cs` | 238 |


| AutotraderScraper | `src/.../Infrastructure/Services/Scrapers/AutotraderScraper.cs` | 271 |


| CarFaxScraper | `src/.../Infrastructure/Services/Scrapers/CarFaxScraper.cs` | 246 |


| CarGurusScraper | `src/.../Infrastructure/Services/Scrapers/CarGurusScraper.cs` | 247 |


| CarMaxScraper | `src/.../Infrastructure/Services/Scrapers/CarMaxScraper.cs` | 242 |


| CarvanaScraper | `src/.../Infrastructure/Services/Scrapers/CarvanaScraper.cs` | 235 |


| ClutchScraper | `src/.../Infrastructure/Services/Scrapers/ClutchScraper.cs` | 324 |


| KijijiScraper | `src/.../Infrastructure/Services/Scrapers/KijijiScraper.cs` | 427 |


| TrueCarScraper | `src/.../Infrastructure/Services/Scrapers/TrueCarScraper.cs` | 239 |


| VroomScraper | `src/.../Infrastructure/Services/Scrapers/VroomScraper.cs` | 224 |


 


#### 1.3.4 ParseLocation (7 Duplicates)


 


| Scraper Class | File Path | Line |


|---------------|-----------|------|


| Auto123Scraper | `src/.../Infrastructure/Services/Scrapers/Auto123Scraper.cs` | 254 |


| AutotraderScraper | `src/.../Infrastructure/Services/Scrapers/AutotraderScraper.cs` | 289 |


| CarFaxScraper | `src/.../Infrastructure/Services/Scrapers/CarFaxScraper.cs` | 262 |


| CarMaxScraper | `src/.../Infrastructure/Services/Scrapers/CarMaxScraper.cs` | 258 |


| ClutchScraper | `src/.../Infrastructure/Services/Scrapers/ClutchScraper.cs` | 340 |


| KijijiScraper | `src/.../Infrastructure/Services/Scrapers/KijijiScraper.cs` | 448 |


| TrueCarScraper | `src/.../Infrastructure/Services/Scrapers/TrueCarScraper.cs` | 255 |


 


#### 1.3.5 ParseTitle (9 Duplicates)


 


| Scraper Class | File Path | Line |


|---------------|-----------|------|


| Auto123Scraper | `src/.../Infrastructure/Services/Scrapers/Auto123Scraper.cs` | 265 |


| AutotraderScraper | `src/.../Infrastructure/Services/Scrapers/AutotraderScraper.cs` | 300 |


| CarFaxScraper | `src/.../Infrastructure/Services/Scrapers/CarFaxScraper.cs` | 273 |


| CarMaxScraper | `src/.../Infrastructure/Services/Scrapers/CarMaxScraper.cs` | 269 |


| CarvanaScraper | `src/.../Infrastructure/Services/Scrapers/CarvanaScraper.cs` | 251 |


| ClutchScraper | `src/.../Infrastructure/Services/Scrapers/ClutchScraper.cs` | 351 |


| KijijiScraper | `src/.../Infrastructure/Services/Scrapers/KijijiScraper.cs` | 465 |


| TrueCarScraper | `src/.../Infrastructure/Services/Scrapers/TrueCarScraper.cs` | 266 |


| VroomScraper | `src/.../Infrastructure/Services/Scrapers/VroomScraper.cs` | 240 |


 


---


 


## 2. Unused Code Analysis


 


### 2.1 Unregistered CLI Commands (5 Commands)


 


These commands are fully implemented but NOT registered in `Program.cs` (lines 176-279):


 


| Command | File Path | Description | Impact |


|---------|-----------|-------------|--------|


| AlertCommand | `src/.../Cli/Commands/AlertCommand.cs` | Alert management | Users cannot access alert functionality |


| CompareCommand | `src/.../Cli/Commands/CompareCommand.cs` | Listing comparison | Feature inaccessible |


| DashboardCommand | `src/.../Cli/Commands/DashboardCommand.cs` | Real-time dashboard | Feature inaccessible |


| ReviewCommand | `src/.../Cli/Commands/ReviewCommand.cs` | Dedup review queue | Feature inaccessible |


| WatchCommand | `src/.../Cli/Commands/WatchCommand.cs` | Watch list management | Feature inaccessible |


 


---


 


### 2.2 Unregistered Core Services (5 Services)


 


These services are implemented but never added to DI container in `Program.cs`:


 


| Service Interface | Implementation File | Lines | Status |


|-------------------|---------------------|-------|--------|


| INewListingDetectionService | `src/.../Core/Services/NewListingDetectionService.cs` | 6-48 | Never registered |


| IPriceChangeDetectionService | `src/.../Core/Services/PriceChangeDetectionService.cs` | 8-89 | Never registered |


| IListingDeactivationService | `src/.../Core/Services/ListingDeactivationService.cs` | 7-79 | Never registered |


| IDealerTrackingService | `src/.../Core/Services/DealerTrackingService.cs` | 8-177 | Never registered |


| IVehicleLinkingService | `src/.../Core/Services/VehicleLinkingService.cs` | 11-151 | Never registered |


 


---


 


### 2.3 Unregistered Infrastructure Services (6 Services)


 


| Service Interface | Implementation File | Status |


|-------------------|---------------------|--------|


| IResponseCacheService | `src/.../Infrastructure/Services/Cache/ResponseCacheService.cs` | Not registered |


| IDebugCaptureService | `src/.../Infrastructure/Services/Debug/DebugCaptureService.cs` | Not registered |


| IHeaderConfigurationService | `src/.../Infrastructure/Services/Headers/HeaderConfigurationService.cs` | Not registered |


| IProxyRotationService | `src/.../Infrastructure/Services/Proxy/ProxyRotationService.cs` | Not registered |


| IProxyService | `src/.../Infrastructure/Services/Proxy/ProxyService.cs` | Not registered |


| IBatchDeduplicationService | `src/.../Infrastructure/Services/Deduplication/BatchDeduplicationService.cs` | Not registered |


 


---


 


### 2.4 Registered But Effectively Unused Service


 


| Service | Issue | Details |


|---------|-------|---------|


| IRelistedDetectionService | Registered at `Program.cs:125` but no CLI command uses it | Service is available in DI but has no entry point |


 


---


 


### 2.5 Missing Required Service Registration


 


| Service | Used By | Issue |


|---------|---------|-------|


| IScraperHealthService | DashboardCommand (line 15, 21), StatusCommand (line 15, 21) | Commands expect this service but it's NOT registered in DI |


 


---


 


### 2.6 Unimplemented Features with TODO Comments


 


**File:** `src/AutomatedMarketIntelligenceTool.Core/Services/AlertService.cs`


 


| Feature | Lines | Current Behavior |


|---------|-------|------------------|


| Email Notifications | 167-171 | Logs warning, marks notification as failed |


| Webhook Notifications | 173-177 | Logs warning, marks notification as failed |


 


```csharp


case NotificationMethod.Email:


    // TODO: Implement email notification


    _logger.LogWarning("Email notifications not yet implemented");


    notification.MarkAsFailed("Email notifications not implemented");


    break;


 


case NotificationMethod.Webhook:


    // TODO: Implement webhook notification


    _logger.LogWarning("Webhook notifications not yet implemented");


    notification.MarkAsFailed("Webhook notifications not implemented");


    break;


```


 


---


 


### 2.7 Unused Enum Values


 


**File:** `src/AutomatedMarketIntelligenceTool.Core/Models/AlertAggregate/NotificationMethod.cs`


 


| Enum Value | Implementation Status |


|------------|----------------------|


| Console | Implemented |


| Email | NOT IMPLEMENTED - logs warning, fails |


| Webhook | NOT IMPLEMENTED - logs warning, fails |


 


---


 


### 2.8 Intentional Exception Methods (Design Pattern)


 


These methods throw `NotImplementedException` by design to enforce parameterized method usage:


 


| Class | Method | File | Purpose |


|-------|--------|------|---------|


| GeoDistanceCalculator | `Calculate()` | `src/.../Core/Services/FuzzyMatching/GeoDistanceCalculator.cs:11-14` | Forces `Calculate(lat1, lon1, lat2, lon2, toleranceMiles)` |


| NumericProximityCalculator | `Calculate()` | `src/.../Core/Services/FuzzyMatching/NumericProximityCalculator.cs:9-12` | Forces `Calculate(decimal, decimal, decimal)` |


| LevenshteinCalculator | `Calculate()` | `src/.../Core/Services/FuzzyMatching/LevenshteinCalculator.cs:9-12` | Forces `Calculate(string, string)` |


 


---


 


## 3. Code Without Requirement Traceability


 


### 3.1 Web Scrapers Beyond Specification


 


**Requirement (REQ-WS-001):** "MVP shall support 2 major Canadian sites (Autotrader.ca, Kijiji.ca)"


 


| Scraper | File | Phase Required | Issue |


|---------|------|----------------|-------|


| AutotraderScraper | `AutotraderScraper.cs` | Phase 1 | Required |


| KijijiScraper | `KijijiScraper.cs` | Phase 1 | Required |


| Auto123Scraper | `Auto123Scraper.cs` | Phase 5 | Beyond MVP |


| CarFaxScraper | `CarFaxScraper.cs` | Not specified | No requirement |


| CarGurusScraper | `CarGurusScraper.cs` | Not specified | No requirement |


| CarMaxScraper | `CarMaxScraper.cs` | Not specified | US-based, spec requires Canadian |


| CarvanaScraper | `CarvanaScraper.cs` | Not specified | US-based, spec requires Canadian |


| ClutchScraper | `ClutchScraper.cs` | Not specified | No requirement |


| TrueCarScraper | `TrueCarScraper.cs` | Not specified | No requirement |


| VroomScraper | `VroomScraper.cs` | Not specified | No requirement |


 


**Impact:** 8 scrapers (80%) implemented beyond MVP scope.


 


---


 


### 3.2 Database Aggregates Beyond MVP Requirements


 


**Phase 1 Requirements:** Listings table (SearchSessions optional)


 


| Aggregate | Location | Phase Required | Status |


|-----------|----------|----------------|--------|


| ListingAggregate | `src/.../Core/Models/ListingAggregate/` | Phase 1 | Required |


| SearchSessionAggregate | `src/.../Core/Models/SearchSessionAggregate/` | Phase 1-2 | Optional |


| AlertAggregate | `src/.../Core/Models/AlertAggregate/` | Not in MVP | No requirement |


| CacheAggregate | `src/.../Core/Models/CacheAggregate/` | Phase 5 | Premature |


| CustomMarketAggregate | `src/.../Core/Models/CustomMarketAggregate/` | Phase 5 | Premature |


| DealerAggregate | `src/.../Core/Models/DealerAggregate/` | Phase 2 | Premature |


| DeduplicationAuditAggregate | `src/.../Core/Models/DeduplicationAuditAggregate/` | Phase 5 | Premature |


| DeduplicationConfigAggregate | `src/.../Core/Models/DeduplicationConfigAggregate/` | Phase 3 | Premature |


| PriceHistoryAggregate | `src/.../Core/Models/PriceHistoryAggregate/` | Phase 3 | Premature |


| ReportAggregate | `src/.../Core/Models/ReportAggregate/` | Phase 2-5 | Premature |


| ResourceThrottleAggregate | `src/.../Core/Models/ResourceThrottleAggregate/` | Phase 5 | Premature |


| ReviewQueueAggregate | `src/.../Core/Models/ReviewQueueAggregate/` | Phase 4 | Premature |


| ScheduledReportAggregate | `src/.../Core/Models/ScheduledReportAggregate/` | Phase 3-4 | Premature |


| ScraperHealthAggregate | `src/.../Core/Models/ScraperHealthAggregate/` | Phase 4 | Premature |


| VehicleAggregate | `src/.../Core/Models/VehicleAggregate/` | Phase 3 | Premature |


| WatchListAggregate | `src/.../Core/Models/WatchListAggregate/` | Phase 3-4 | Premature |


| ScrapedListingAggregate | `src/.../Core/Models/ScrapedListingAggregate/` | Unknown | Not documented |


 


**Impact:** 15 extra aggregates beyond Phase 1 requirements.


 


---


 


### 3.3 CLI Commands Beyond Specification


 


**Phase 1 Requirements:** search, scrape (core MVP)


 


| Command | Phase Required | Registered | Requirement ID |


|---------|----------------|------------|----------------|


| search | Phase 1 | Yes | REQ-CLI-001.2 |


| scrape | Phase 1 | Yes | MVP core |


| list | Phase 2 | Yes | REQ-CLI-001.3 |


| export | Phase 2 | Yes | REQ-CLI-001.4 |


| config | Phase 2 | Yes | REQ-CLI-001.5 |


| profile | Phase 3 | Yes | REQ-CLI-001.6 |


| status | Phase 3 | Yes | REQ-CLI-001.7 |


| completion | Phase 4 | Yes | REQ-CLI-011 |


| show | Unknown | Yes | No requirement found |


| stats | Phase 3 | Yes | Not explicit |


| import | Phase 3+ | Yes | Not explicit |


| backup | Phase 3+ | Yes | Not explicit |


| restore | Phase 3+ | Yes | Not explicit |


| report | Phase 5 | Yes | REQ-RP-* |


| audit | Phase 5 | Yes | Phase 5 feature |


| market | Phase 5 | Yes | Phase 5 feature |


| schedule | Phase 4 | Yes | REQ-CLI-014 |


| throttle | Phase 5 | Yes | Phase 5 feature |


| alert | Unknown | NO | No requirement |


| dashboard | Phase 5 | NO | Phase 5 feature |


| watch | Phase 3-4 | NO | Phase 3-4 feature |


| review | Phase 4 | NO | Phase 4 feature |


| compare | Unknown | NO | No requirement found |


 


**Impact:** 16 commands beyond Phase 1 scope (13 registered, 5 unregistered).


 


---


 


### 3.4 Services Beyond Phase 1 Requirements


 


**Phase 1 Required Services:** SearchService, DuplicateDetectionService, basic rate limiting


 


| Service Category | Count | Phase Required |


|------------------|-------|----------------|


| Core Phase 1 Services | 4 | Phase 1 |


| Image Analysis Services | 3 | Phase 4 |


| Fuzzy Matching Services | 6 | Phase 3 |


| Reporting Services | 5+ | Phase 2-5 |


| Dashboard Services | 2 | Phase 5 |


| Scheduling Services | 2 | Phase 3-4 |


| Deduplication Services | 4 | Phase 5 |


| Custom Market Services | 2 | Phase 5 |


| Resource Throttle Services | 1 | Phase 5 |


| Alert Services | 1 | Not in specs |


| Watch List Services | 1 | Phase 3-4 |


| Review Services | 1 | Phase 4 |


| Vehicle Linking Services | 1 | Phase 3 |


| Dealer Tracking Services | 1 | Phase 2 |


| Detection Services | 3 | Phase 2-3 |


 


**Total:** 35+ services when Phase 1 requires ~4.


 


---


 


### 3.5 Files With No Requirement Traceability


 


| File Path | Feature | Issue |


|-----------|---------|-------|


| `src/.../Cli/Commands/CompareCommand.cs` | Listing comparison | No REQ-* reference found |


| `src/.../Cli/Commands/AlertCommand.cs` | Alert management | Not in Phase 1-5 specs |


| `src/.../Core/Models/CustomMarketAggregate/` | Custom market regions | Beyond current phase |


| `src/.../Infrastructure/Services/Headers/HeaderConfigurationService.cs` | HTTP header config | REQ-WS-013 (Phase 5) |


| `src/.../Infrastructure/Services/Proxy/ProxyService.cs` | Proxy management | REQ-WS-007 (Phase 3) |


| `src/.../Infrastructure/Services/Cache/ResponseCacheService.cs` | Response caching | REQ-WS-014 (Phase 5) |


| `src/.../Infrastructure/Services/Scraping/ConcurrentScrapingEngine.cs` | Parallel scraping | REQ-WS-011 (Phase 4) |


| `src/.../Core/Services/ImageAnalysis/` (3 files) | Image hashing | REQ-DD-004 (Phase 4) |


 


---


 


## 4. Summary Statistics


 


### 4.1 Duplicated Code


 


| Category | Count | Description |


|----------|-------|-------------|


| CSV Escaping | 2 | Identical logic in CsvFormatter and ExportCommand |


| File Sanitization | 2 | Identical logic in DebugCaptureService and ScheduledReport |


| ExtractExternalId | 10 | One per scraper |


| ParsePrice | 10 | One per scraper |


| ParseMileage | 10 | One per scraper |


| ParseLocation | 7 | One per applicable scraper |


| ParseTitle | 9 | One per scraper |


| **Total Duplicates** | **50+** | |


 


### 4.2 Unused Code


 


| Category | Count | Description |


|----------|-------|-------------|


| Unregistered CLI Commands | 5 | AlertCommand, CompareCommand, DashboardCommand, ReviewCommand, WatchCommand |


| Unregistered Core Services | 5 | NewListingDetection, PriceChangeDetection, ListingDeactivation, DealerTracking, VehicleLinking |


| Unregistered Infrastructure Services | 6 | ResponseCache, DebugCapture, HeaderConfiguration, ProxyRotation, Proxy, BatchDeduplication |


| Registered but Unused | 1 | IRelistedDetectionService |


| Missing Registration | 1 | IScraperHealthService (required by 2 commands) |


| Unimplemented Features | 2 | Email and Webhook notifications |


| Unused Enum Values | 2 | Email and Webhook in NotificationMethod |


| **Total Unused Elements** | **22+** | |


 


### 4.3 Code Without Requirements


 


| Category | Count | Description |


|----------|-------|-------------|


| Extra Scrapers | 8 | Beyond Phase 1's 2 required |


| Extra Aggregates | 15 | Beyond Phase 1's 1-2 required |


| Extra CLI Commands | 16 | Beyond Phase 1's 2-3 required |


| Extra Services | 30+ | Beyond Phase 1's ~4 required |


| **Total Scope Creep** | **69+ files** | |


 


---


 


## 5. Recommendations


 


### 5.1 High Priority - Duplicated Code


 


1. **Extract scraper helper methods to BaseScraper class:**


   - Move `ParsePrice()`, `ParseMileage()`, `ParseLocation()`, `ParseTitle()`, `ExtractExternalId()` to abstract base class


   - Override only site-specific variations


   - **Estimated reduction:** 40+ duplicate method implementations


 


2. **Create shared StringUtilities class:**


   - Consolidate `EscapeCsv()`, `EscapeCsvValue()`, `SanitizeFileName()`, `SanitizeFilename()`


   - Location: `src/AutomatedMarketIntelligenceTool.Core/Utilities/StringUtilities.cs`


   - **Estimated reduction:** 4 duplicate methods


 


### 5.2 High Priority - Unused Code


 


1. **Register missing CLI commands or remove:**


   - Either add to `Program.cs`: AlertCommand, CompareCommand, DashboardCommand, ReviewCommand, WatchCommand


   - Or delete if not needed for current phase


 


2. **Register missing services or remove:**


   - Add to DI container or delete: NewListingDetectionService, PriceChangeDetectionService, ListingDeactivationService, DealerTrackingService, VehicleLinkingService


 


3. **Fix IScraperHealthService registration:**


   - Add `services.AddScoped<IScraperHealthService, ScraperHealthService>();` to Program.cs


   - Required for DashboardCommand and StatusCommand to function


 


4. **Implement or remove notification methods:**


   - Either implement Email and Webhook notifications


   - Or remove enum values and related code paths


 


### 5.3 Medium Priority - Scope Management


 


1. **Phase-gate features:**


   - Consider disabling Phase 2-5 services/commands until needed


   - Use feature flags or separate registration methods


 


2. **Review scraper necessity:**


   - Evaluate if all 10 scrapers are needed


   - Consider removing US-based scrapers (CarMax, Carvana, Vroom) if Canada-focus is requirement


 


3. **Document undocumented features:**


   - Add requirement IDs for: show, stats, import, backup, restore, compare commands


   - Or mark as non-essential/experimental


 


### 5.4 Low Priority - Technical Debt


 


1. **Consider lazy loading for Phase 4-5 services**


2. **Add dead code detection to CI pipeline**


3. **Implement automatic requirement traceability checking**


 


---


 


## Appendix A: File Paths Reference


 


### CLI Commands Location


`src/AutomatedMarketIntelligenceTool.Cli/Commands/`


 


### Core Services Location


`src/AutomatedMarketIntelligenceTool.Core/Services/`


 


### Infrastructure Services Location


`src/AutomatedMarketIntelligenceTool.Infrastructure/Services/`


 


### Web Scrapers Location


`src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/`


 


### Core Models Location


`src/AutomatedMarketIntelligenceTool.Core/Models/`


 


---


 


## Appendix B: Requirements Documents Referenced


 


- `docs/specs/implementation-specs.md` - System architecture (REQ-SYS-*, REQ-CORE-*, REQ-INFRA-*, REQ-API-*)


- `docs/specs/cli-interface/cli-interface.specs.md` - CLI requirements (REQ-CLI-*)


- `docs/specs/web-scraping/web-scraping.specs.md` - Web scraping (REQ-WS-*)


- `docs/specs/duplicate-detection/duplicate-detection.specs.md` - Deduplication (REQ-DD-*)


- `docs/specs/data-persistence/data-persistence.specs.md` - Persistence (REQ-DP-*)


- `docs/specs/reporting/reporting.specs.md` - Reporting (REQ-RP-*)


- `docs/roadmaps/phase-1/README.md` through `phase-5/README.md` - Phase features


 


---


 


**End of Report**