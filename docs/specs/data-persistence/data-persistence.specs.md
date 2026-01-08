# Data Persistence Feature Requirements

## Overview

The Data Persistence feature manages the storage and retrieval of car listing data using Entity Framework Core with a relational database. This feature ensures that search results are persisted reliably, enabling historical tracking, duplicate detection, and data analysis.

---

## REQ-DP-001: Database Provider Support

**Phase: 1 (MVP) / 2**

### Formal Statement

The system SHALL support multiple database providers through Entity Framework Core.

### Acceptance Criteria

- [ ] AC-001.1: SQLite supported as default database provider (zero configuration) (Phase 1)
- [ ] AC-001.2: SQL Server supported for enterprise deployments (Phase 2)
- [ ] AC-001.3: PostgreSQL supported as alternative provider (Phase 3)
- [ ] AC-001.4: Database provider configurable via connection string or config file (Phase 2)
- [ ] AC-001.5: Database migrations handled automatically on startup (Phase 1)
- [ ] AC-001.6: User can specify custom database path for SQLite (e.g., `--database /path/to/db.sqlite`) (Phase 2)

---

## REQ-DP-002: Listing Entity Schema

**Phase: 1 (MVP) / 2**

### Formal Statement

The system SHALL define a comprehensive data model for car listings that captures all relevant attributes.

### Acceptance Criteria

- [ ] AC-002.1: Entity includes: Id (GUID), ExternalId, SourceSite, ListingUrl (Phase 1)
- [ ] AC-002.2: Entity includes: Make, Model, Year, Trim, Price, Mileage (Phase 1)
- [ ] AC-002.3: Entity includes: VIN, Condition, Transmission, FuelType, Drivetrain (Phase 2)
- [ ] AC-002.4: Entity includes: BodyStyle, ExteriorColor, InteriorColor (Phase 2)
- [ ] AC-002.5: Entity includes: City, State, ZipCode, Latitude, Longitude (Phase 1)
- [ ] AC-002.6: Entity includes: SellerType, SellerName, SellerPhone (Phase 2)
- [ ] AC-002.7: Entity includes: Description, ImageUrls (JSON array), Features (JSON array) (Phase 2)
- [ ] AC-002.8: Entity includes: ListingDate, FirstSeenDate, LastSeenDate, IsActive (Phase 1)
- [ ] AC-002.9: Entity includes: CreatedAt, UpdatedAt timestamps (Phase 1)

---

## REQ-DP-003: Listing Insertion

**Phase: 1 (MVP)**

### Formal Statement

The system SHALL insert new car listings into the database with proper validation.

### Acceptance Criteria

- [ ] AC-003.1: New listings inserted with generated GUID (Phase 1)
- [ ] AC-003.2: FirstSeenDate set to current UTC timestamp on insert (Phase 1)
- [ ] AC-003.3: LastSeenDate updated on every observation (Phase 1)
- [ ] AC-003.4: Duplicate detection performed before insertion (see REQ-DD) (Phase 1)
- [ ] AC-003.5: Batch insertion supported for performance (configurable batch size) (Phase 2)
- [ ] AC-003.6: Transaction used to ensure atomicity of batch operations (Phase 2)

---

## REQ-DP-004: Listing Updates

**Phase: 2**

### Formal Statement

The system SHALL update existing listings when changes are detected.

### Acceptance Criteria

- [ ] AC-004.1: Price changes tracked and recorded (Phase 2)
- [ ] AC-004.2: UpdatedAt timestamp modified on any field change (Phase 2)
- [ ] AC-004.3: LastSeenDate updated even when no changes detected (Phase 2)
- [ ] AC-004.4: Historical price data preserved (see REQ-DP-006) (Phase 3)
- [ ] AC-004.5: Changed fields logged for audit purposes (Phase 3)

---

## REQ-DP-005: Listing Deactivation

**Phase: 2**

### Formal Statement

The system SHALL mark listings as inactive when they are no longer found on source sites.

### Acceptance Criteria

- [ ] AC-005.1: Listings not seen for configurable period marked as inactive (default 7 days) (Phase 2)
- [ ] AC-005.2: IsActive flag set to false for inactive listings (Phase 2)
- [ ] AC-005.3: Inactive listings retained in database for historical reference (Phase 2)
- [ ] AC-005.4: User can purge inactive listings older than specified age (Phase 3)
- [ ] AC-005.5: Reappearing listings are reactivated with IsActive set to true (Phase 2)

---

## REQ-DP-006: Price History Tracking

**Phase: 3**

### Formal Statement

The system SHALL maintain a history of price changes for each listing.

### Acceptance Criteria

- [ ] AC-006.1: PriceHistory entity tracks: ListingId, Price, ObservedAt (Phase 3)
- [ ] AC-006.2: New price history record created on each price change (Phase 3)
- [ ] AC-006.3: Price history queryable by listing ID (Phase 3)
- [ ] AC-006.4: Price trend (increasing/decreasing/stable) calculable from history (Phase 3)
- [ ] AC-006.5: Days on market calculable from first and last price history entries (Phase 3)

---

## REQ-DP-007: Search Session Logging

**Phase: 2**

### Formal Statement

The system SHALL log each search session with its parameters and results summary.

### Acceptance Criteria

- [ ] AC-007.1: SearchSession entity tracks: Id, StartTime, EndTime, Status (Phase 2)
- [ ] AC-007.2: SearchSession includes: SearchParameters (JSON), ResultCount, NewListingsCount (Phase 2)
- [ ] AC-007.3: SearchSession linked to listings found during session (Phase 2)
- [ ] AC-007.4: Failed sessions logged with error details (Phase 2)
- [ ] AC-007.5: Session history queryable for reporting (Phase 3)

---

## REQ-DP-008: Database Querying

**Phase: 1 (MVP) / 2**

### Formal Statement

The system SHALL provide efficient querying capabilities for stored listings.

### Acceptance Criteria

- [ ] AC-008.1: Query by any combination of listing attributes (Phase 2)
- [ ] AC-008.2: Full-text search on description field supported (Phase 3)
- [ ] AC-008.3: Date range queries supported (first seen, last seen, listing date) (Phase 2)
- [ ] AC-008.4: Price range queries supported (Phase 1)
- [ ] AC-008.5: Pagination supported for large result sets (Phase 1)
- [ ] AC-008.6: Sorting by any field supported (Phase 2)
- [ ] AC-008.7: Indexes created on commonly queried fields (Phase 2)

---

## REQ-DP-009: Data Export

**Phase: 3**

### Formal Statement

The system SHALL support exporting stored data in various formats.

### Acceptance Criteria

- [ ] AC-009.1: Export to CSV format (e.g., `--export csv`) (Phase 3)
- [ ] AC-009.2: Export to JSON format (e.g., `--export json`) (Phase 3)
- [ ] AC-009.3: Export to Excel format (e.g., `--export xlsx`) (Phase 5)
- [ ] AC-009.4: Export filters supported (same as query filters) (Phase 3)
- [ ] AC-009.5: Export includes all or selected fields (Phase 3)
- [ ] AC-009.6: Large exports streamed to avoid memory issues (Phase 4)

---

## REQ-DP-010: Data Import

**Phase: 4**

### Formal Statement

The system SHALL support importing listing data from external sources.

### Acceptance Criteria

- [ ] AC-010.1: Import from CSV format with header mapping (Phase 4)
- [ ] AC-010.2: Import from JSON format (Phase 4)
- [ ] AC-010.3: Duplicate detection applied during import (Phase 4)
- [ ] AC-010.4: Import validation with error reporting (Phase 4)
- [ ] AC-010.5: Dry-run mode to preview import without committing (Phase 4)

---

## REQ-DP-011: Database Backup and Restore

**Phase: 4**

### Formal Statement

The system SHALL provide database backup and restore capabilities.

### Acceptance Criteria

- [ ] AC-011.1: Manual backup command available (e.g., `car-search backup`) (Phase 4)
- [ ] AC-011.2: Automatic backup before destructive operations (Phase 4)
- [ ] AC-011.3: Backup files timestamped and stored in configurable location (Phase 4)
- [ ] AC-011.4: Restore command available (e.g., `car-search restore <backup-file>`) (Phase 4)
- [ ] AC-011.5: Backup retention policy configurable (default keep last 5) (Phase 4)

---

## REQ-DP-012: Data Integrity Constraints

**Phase: 4**

### Formal Statement

The system SHALL enforce data integrity through database constraints.

### Acceptance Criteria

- [ ] AC-012.1: Unique constraint on (ExternalId, SourceSite) combination (Phase 4)
- [ ] AC-012.2: Foreign key constraints enforced for related entities (Phase 4)
- [ ] AC-012.3: Required fields enforced at database level (Phase 4)
- [ ] AC-012.4: Check constraints on numeric ranges (Year, Price, Mileage) (Phase 4)
- [ ] AC-012.5: Cascade delete configured appropriately for related data (Phase 4)

---

## REQ-DP-013: Connection Resilience

**Phase: 4**

### Formal Statement

The system SHALL handle database connection issues gracefully.

### Acceptance Criteria

- [ ] AC-013.1: Connection retry with exponential backoff on transient failures (Phase 4)
- [ ] AC-013.2: Connection pooling configured for performance (Phase 4)
- [ ] AC-013.3: Timeout configured to avoid hanging operations (Phase 4)
- [ ] AC-013.4: Clear error messages for connection failures (Phase 4)
- [ ] AC-013.5: Graceful degradation when database unavailable (Phase 4)

---

## REQ-DP-014: Performance Optimization

**Phase: 4**

### Formal Statement

The system SHALL optimize database operations for performance.

### Acceptance Criteria

- [ ] AC-014.1: Batch operations use configurable batch size (default 100) (Phase 4)
- [ ] AC-014.2: Async operations used throughout (Phase 4)
- [ ] AC-014.3: No N+1 query patterns in data access layer (Phase 4)
- [ ] AC-014.4: Query execution plans optimized with proper indexing (Phase 4)
- [ ] AC-014.5: Memory-efficient streaming for large data sets (Phase 4)
