# Canada-Only Tool: Required Changes

This document itemizes all changes required to convert the Automated Market Intelligence Tool to a Canada-only application, ordered by implementation priority.

---

## Priority 1: Core Data Models (Foundation)

These changes must happen first as all other components depend on them.

### 1.1 Add Canadian Province Enum
- **File**: `src/AutomatedMarketIntelligenceTool.Core/Models/ListingAggregate/Enums/CanadianProvince.cs` (NEW)
- **Change**: Create enum with values: `AB, BC, MB, NB, NL, NS, NT, NU, ON, PE, QC, SK, YT`
- **Reason**: Foundation for all location-based logic

### 1.2 Update SearchParameters Model
- **File**: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/SearchParameters.cs`
- **Changes**:
  - Replace `ZipCode` property with `PostalCode`
  - Replace `RadiusMiles` with `RadiusKilometers`
  - Add `Province` property (CanadianProvince enum)
  - Add postal code validation (format: `A1A 1A1`)
- **Reason**: Required for Canadian scraper queries

### 1.3 Update ScrapedListing Model
- **File**: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/ScrapedListing.cs`
- **Changes**:
  - Replace `State` property with `Province`
  - Replace `ZipCode` property with `PostalCode`
  - Add `Currency` property (default: "CAD")
  - Add `Country` property (default: "CA")
- **Reason**: Required to store Canadian listing data from scrapers

### 1.4 Update Listing Domain Model
- **File**: `src/AutomatedMarketIntelligenceTool.Core/Models/ListingAggregate/Listing.cs`
- **Changes**:
  - Replace `State` property with `Province`
  - Replace `ZipCode` property with `PostalCode`
  - Add `Currency` property (default: "CAD")
  - Update `Create()` factory method signature
- **Reason**: Core domain model must reflect Canadian data structure

### 1.5 Update Entity Configuration
- **File**: `src/AutomatedMarketIntelligenceTool.Infrastructure/EntityConfigurations/ListingConfiguration.cs`
- **Changes**:
  - Configure `Province` property (max length 2)
  - Configure `PostalCode` property (max length 7 for `A1A 1A1` format)
  - Remove `State` and `ZipCode` configurations
- **Reason**: Database schema must match domain model

### 1.6 Create Database Migration
- **File**: `src/AutomatedMarketIntelligenceTool.Infrastructure/Migrations/` (NEW migration)
- **Changes**:
  - Add migration to rename `State` → `Province`
  - Add migration to rename `ZipCode` → `PostalCode`
  - Add `Currency` column with default "CAD"
  - Adjust column lengths appropriately
- **Reason**: Persist schema changes to database

---

## Priority 2: Scrapers (Core Functionality)

These changes implement the Canada-specific data collection.

### 2.1 Replace AutotraderScraper with Autotrader.ca
- **File**: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/AutotraderScraper.cs`
- **Changes**:
  - Update base URL from `https://www.autotrader.com` to `https://www.autotrader.ca`
  - Update URL builder to use postal codes instead of ZIP codes
  - Update query parameters for Canadian site structure
  - Update CSS selectors/parsing for Canadian site DOM
  - Parse Canadian province codes instead of US states
  - Handle CAD currency format
  - Update radius to use kilometers
- **Reason**: Primary Canadian automotive listing source

### 2.2 Replace CarsComScraper with Kijiji.ca Scraper
- **File**: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/CarsComScraper.cs` → Rename to `KijijiScraper.cs`
- **Changes**:
  - Complete rewrite targeting `https://www.kijiji.ca/b-cars-vehicles/`
  - Implement Kijiji-specific URL structure
  - Parse Kijiji listing format (price, mileage, location)
  - Handle Kijiji's location-based filtering
  - Extract Canadian postal codes and provinces
- **Reason**: Kijiji is Canada's largest classifieds site for vehicles

### 2.3 Update ScraperFactory
- **File**: `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/ScraperFactory.cs`
- **Changes**:
  - Update source enum/options to include Canadian sources only
  - Remove Cars.com reference, add Kijiji
  - Update factory method to instantiate Canadian scrapers
- **Reason**: Factory must create appropriate Canadian scrapers

### 2.4 (Optional) Add Additional Canadian Scrapers
- **Files**: New scraper files in `src/AutomatedMarketIntelligenceTool.Infrastructure/Services/Scrapers/`
- **Potential scrapers**:
  - `FacebookMarketplaceScraper.cs` - Facebook Marketplace Canada
  - `Auto123Scraper.cs` - Auto123.com (Canadian site)
  - `CraigslistCaScraper.cs` - Craigslist.ca
- **Reason**: Expand Canadian market coverage

---

## Priority 3: Specifications & Configuration

Update specifications to reflect Canada-only scope.

### 3.1 Update Location Configuration Specification
- **File**: `docs/specs/location-configuration/location-configuration.specs.md`
- **Changes**:
  - REQ-LC-001: Replace ZIP code with postal code as primary input
  - REQ-LC-002: Replace state support with province support
  - REQ-LC-003: Change default radius unit from miles to kilometers
  - REQ-LC-010: Remove country selection (Canada only)
  - Update all acceptance criteria with Canadian examples
  - Remove "Phase 2" labels for Canadian features (now Phase 1)
- **Reason**: Specs must reflect Canada-only scope

### 3.2 Update Search Configuration Specification
- **File**: `docs/specs/search-configuration/search-configuration.specs.md`
- **Changes**:
  - REQ-SC-004: Explicitly state CAD currency
  - REQ-SC-005: Change default unit from miles to kilometers
  - Update all examples with Canadian context
- **Reason**: Search parameters must use Canadian defaults

### 3.3 Update Web Scraping Specification
- **File**: `docs/specs/web-scraping/web-scraping.specs.md`
- **Changes**:
  - REQ-WS-001: Replace US sites with Canadian equivalents
  - Update supported platforms list to: Autotrader.ca, Kijiji.ca, Facebook Marketplace Canada, Auto123, Craigslist.ca
  - Update CSS selector examples for Canadian sites
  - Update rate limiting guidelines for Canadian sites
- **Reason**: Scraping targets must be Canadian sites only

### 3.4 Update CLI Interface Specification
- **File**: `docs/specs/cli-interface/cli-interface.specs.md`
- **Changes**:
  - Update `--location` examples to use Canadian cities
  - Add `--postal-code` option (replace `--zip`)
  - Add `--province` option (replace `--state`)
  - Update help text with Canadian terminology
  - Change default radius unit display to kilometers
- **Reason**: CLI must use Canadian terminology

### 3.5 Update Data Persistence Specification
- **File**: `docs/specs/data-persistence/data-persistence.specs.md`
- **Changes**:
  - Update schema examples with Province/PostalCode
  - Add Currency field documentation
  - Update example data with Canadian listings
- **Reason**: Persistence documentation must match new schema

---

## Priority 4: Roadmap Documentation

Update all roadmap documents to reflect Canadian scope.

### 4.1 Update Phase 1 Roadmap
- **File**: `docs/roadmaps/phase-1/README.md`
- **Changes**:
  - Replace Autotrader (US) → Autotrader.ca
  - Replace Cars.com → Kijiji.ca
  - Update all code examples to use postal codes
  - Update location examples to Canadian cities (Toronto, Vancouver, Montreal)
  - Update radius examples to kilometers
- **Reason**: Phase 1 targets must be Canadian

### 4.2 Update Phase 2 Roadmap
- **File**: `docs/roadmaps/phase-2/README.md`
- **Changes**:
  - Update duplicate detection for Canadian listings
  - Update city examples to Canadian cities
  - Update currency references to CAD
- **Reason**: Phase 2 must handle Canadian data

### 4.3 Update Phase 3 Roadmap
- **File**: `docs/roadmaps/phase-3/README.md`
- **Changes**:
  - Replace planned US sites with Canadian equivalents:
    - CarMax → N/A (US only)
    - Carvana → N/A (US only)
    - Vroom → N/A (US only)
    - TrueCar → N/A (US only)
    - eBay Motors → Facebook Marketplace Canada
    - Craigslist → Craigslist.ca
  - Add Canadian alternatives: Auto123, Used.ca, local dealer sites
- **Reason**: Multi-source expansion must target Canadian sites

### 4.4 Update Phase 4 Roadmap
- **File**: `docs/roadmaps/phase-4/README.md`
- **Changes**:
  - Remove REQ-LC-010 country selection option
  - Remove US market references
  - Update examples to Canada-only context
- **Reason**: Advanced features apply to Canada only

### 4.5 Update Phase 5 Roadmap
- **File**: `docs/roadmaps/phase-5/README.md`
- **Changes**:
  - Update custom regions to Canadian markets:
    - "greater-toronto-area" (GTA)
    - "metro-vancouver"
    - "greater-montreal"
    - "calgary-edmonton-corridor"
  - Replace ZIP code examples with postal code examples
  - Update all geographic references to Canadian
- **Reason**: Custom regions must be Canadian

---

## Priority 5: Main Documentation

Update user-facing documentation.

### 5.1 Update Main README
- **File**: `README.md`
- **Changes**:
  - Update project description to state "Canada-only automotive market intelligence"
  - Update example commands:
    - `--location "New York"` → `--location "Toronto"`
    - Add `--postal-code M5V3L9` examples
  - Update price examples to reflect CAD values
  - Update supported sources list to Canadian sites
  - Add note about Canadian-only scope
- **Reason**: Primary user documentation must reflect scope

### 5.2 Update Implementation Specs
- **File**: `docs/specs/implementation-specs.md`
- **Changes**:
  - Review and update any US-specific references
  - Update validation rules for Canadian postal codes
  - Update architecture diagrams if they reference US sites
- **Reason**: Implementation guidance must be Canadian-specific

---

## Priority 6: Tests

Update all tests to use Canadian data and validate Canadian behavior.

### 6.1 Update AutotraderScraper Tests
- **File**: `tests/AutomatedMarketIntelligenceTool.Infrastructure.Tests/Services/Scrapers/AutotraderScraperTests.cs`
- **Changes**:
  - Update mock HTML to match Autotrader.ca structure
  - Replace ZIP codes with postal codes in test data
  - Replace US states with Canadian provinces
  - Update expected URLs to Autotrader.ca format
  - Add tests for postal code validation
- **Reason**: Tests must validate Canadian scraper behavior

### 6.2 Create/Update Kijiji Scraper Tests
- **File**: `tests/AutomatedMarketIntelligenceTool.Infrastructure.Tests/Services/Scrapers/KijijiScraperTests.cs` (NEW or renamed from CarsComScraperTests.cs)
- **Changes**:
  - Create mock HTML matching Kijiji.ca structure
  - Test postal code-based location queries
  - Test province parsing
  - Test CAD price parsing
  - Test kilometer-based mileage parsing
- **Reason**: New scraper requires comprehensive tests

### 6.3 Update SearchParameters Tests
- **File**: `tests/AutomatedMarketIntelligenceTool.Infrastructure.Tests/Services/Scrapers/SearchParametersTests.cs`
- **Changes**:
  - Replace ZIP code tests with postal code tests
  - Add postal code validation tests (valid: `M5V 3L9`, invalid: `12345`)
  - Add province validation tests
  - Update radius tests to use kilometers
- **Reason**: Validate Canadian parameter handling

### 6.4 Update ScrapedListing Tests
- **File**: `tests/AutomatedMarketIntelligenceTool.Infrastructure.Tests/Services/Scrapers/ScrapedListingTests.cs`
- **Changes**:
  - Update test data to use Canadian provinces
  - Update test data to use postal codes
  - Add Currency property tests
- **Reason**: Validate Canadian listing data structure

### 6.5 Update ScraperFactory Tests
- **File**: `tests/AutomatedMarketIntelligenceTool.Infrastructure.Tests/Services/Scrapers/ScraperFactoryTests.cs`
- **Changes**:
  - Update source enums to Canadian sources
  - Test creation of Canadian scrapers
  - Remove Cars.com references
- **Reason**: Validate factory creates Canadian scrapers

### 6.6 Update Listing Domain Model Tests
- **File**: `tests/AutomatedMarketIntelligenceTool.Core.Tests/Models/ListingAggregate/ListingTests.cs`
- **Changes**:
  - Update test data with Canadian locations
  - Test Province property
  - Test PostalCode property
  - Test Currency property defaults to CAD
- **Reason**: Validate Canadian domain model behavior

---

## Priority 7: Configuration Files (If Applicable)

### 7.1 Update appsettings.json
- **File**: `appsettings.json` (if exists)
- **Changes**:
  - Add `DefaultCountry: "CA"`
  - Add `DefaultCurrency: "CAD"`
  - Add `DefaultRadiusUnit: "kilometers"`
  - Add `SupportedProvinces` array
  - Update default location to Canadian postal code
- **Reason**: Runtime configuration for Canadian defaults

---

## Summary: Implementation Order

| Order | Category | Items | Estimated Files |
|-------|----------|-------|-----------------|
| 1 | Core Models | 1.1 - 1.6 | 6 files + 1 migration |
| 2 | Scrapers | 2.1 - 2.4 | 3-6 files |
| 3 | Specifications | 3.1 - 3.5 | 5 files |
| 4 | Roadmaps | 4.1 - 4.5 | 5 files |
| 5 | Documentation | 5.1 - 5.2 | 2 files |
| 6 | Tests | 6.1 - 6.6 | 6 files |
| 7 | Configuration | 7.1 | 1 file |

**Total: ~28-31 files require changes**

---

## Key Canadian Data Formats

### Postal Code Format
- Pattern: `A1A 1A1` (letter-digit-letter space digit-letter-digit)
- Regex: `^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$`
- Examples: `M5V 3L9`, `V6B 1A1`, `H2X 1Y6`

### Province Codes
| Code | Province/Territory |
|------|-------------------|
| AB | Alberta |
| BC | British Columbia |
| MB | Manitoba |
| NB | New Brunswick |
| NL | Newfoundland and Labrador |
| NS | Nova Scotia |
| NT | Northwest Territories |
| NU | Nunavut |
| ON | Ontario |
| PE | Prince Edward Island |
| QC | Quebec |
| SK | Saskatchewan |
| YT | Yukon |

### Canadian Sites to Target
| Site | URL | Type |
|------|-----|------|
| Autotrader.ca | https://www.autotrader.ca | Dealer + Private |
| Kijiji Autos | https://www.kijiji.ca/b-cars-vehicles/ | Classifieds |
| Facebook Marketplace | https://www.facebook.com/marketplace/category/vehicles | Social |
| Auto123 | https://www.auto123.com | Dealer |
| Craigslist.ca | https://[city].craigslist.ca/search/cta | Classifieds |

---

## Notes

1. **Breaking Changes**: Replacing `State`→`Province` and `ZipCode`→`PostalCode` are breaking changes. Consider a database migration strategy.

2. **Kilometer Default**: Canadian vehicles typically display odometer in kilometers. Ensure all mileage parsing and display uses km.

3. **Currency**: All prices should be stored and displayed as CAD. No conversion needed for Canada-only scope.

4. **Bilingual Consideration**: Quebec listings may be in French. Consider parsing French terms for condition, features, etc.
