# Duplicate Detection Feature Requirements

## Overview

The Duplicate Detection feature ensures that car listings are accurately identified as new or existing across multiple data sources. This feature prevents duplicate entries in the database and enables accurate tracking of listing history and changes.

---

## REQ-DD-001: VIN-Based Deduplication

### Formal Statement

The system SHALL use Vehicle Identification Number (VIN) as the primary identifier for duplicate detection when available.

### Acceptance Criteria

- [ ] AC-001.1: Listings with matching VINs are identified as the same vehicle
- [ ] AC-001.2: VIN matching is case-insensitive
- [ ] AC-001.3: VIN format validated (17 characters, alphanumeric excluding I, O, Q)
- [ ] AC-001.4: Partial VINs (last 8 characters) used as fallback match criterion
- [ ] AC-001.5: Multiple listings for same VIN linked to single vehicle record

---

## REQ-DD-002: External ID Deduplication

### Formal Statement

The system SHALL use source site external IDs to identify duplicate listings within the same source.

### Acceptance Criteria

- [ ] AC-002.1: Each source site's listing ID captured as ExternalId
- [ ] AC-002.2: Combination of (SourceSite, ExternalId) is unique
- [ ] AC-002.3: Returning listing with same ExternalId updates existing record
- [ ] AC-002.4: Different ExternalId on same source treated as new listing (unless VIN matches)

---

## REQ-DD-003: Fuzzy Matching

### Formal Statement

The system SHALL implement fuzzy matching to identify likely duplicates across sources when VIN is not available.

### Acceptance Criteria

- [ ] AC-003.1: Fuzzy matching considers: Make, Model, Year, Mileage, Price, Location
- [ ] AC-003.2: Match confidence score calculated (0-100%)
- [ ] AC-003.3: Configurable confidence threshold for automatic deduplication (default 85%)
- [ ] AC-003.4: Near-matches (60-85%) flagged for manual review
- [ ] AC-003.5: Mileage tolerance of ±500 miles considered a match
- [ ] AC-003.6: Price tolerance of ±$500 considered a match
- [ ] AC-003.7: Location within 10 miles considered same location

---

## REQ-DD-004: Image-Based Matching

### Formal Statement

The system SHALL optionally use image comparison to assist in duplicate detection.

### Acceptance Criteria

- [ ] AC-004.1: Image hashing (perceptual hash) used for comparison
- [ ] AC-004.2: Image matching can be enabled/disabled (e.g., `--image-matching`)
- [ ] AC-004.3: Multiple images compared, majority match required
- [ ] AC-004.4: Image matching used to increase confidence score, not as sole criterion
- [ ] AC-004.5: Image matching gracefully degraded if images unavailable

---

## REQ-DD-005: Cross-Source Linking

### Formal Statement

The system SHALL link listings of the same vehicle across different source sites.

### Acceptance Criteria

- [ ] AC-005.1: Vehicle entity created to group related listings
- [ ] AC-005.2: All listings for same vehicle linked via VehicleId foreign key
- [ ] AC-005.3: Best/most complete data aggregated at vehicle level
- [ ] AC-005.4: Price comparison across sources available for same vehicle
- [ ] AC-005.5: User can view all sources for a given vehicle

---

## REQ-DD-006: Duplicate Resolution

### Formal Statement

The system SHALL provide mechanisms to resolve ambiguous duplicates.

### Acceptance Criteria

- [ ] AC-006.1: Automatic resolution for high-confidence matches (>85%)
- [ ] AC-006.2: Manual resolution interface for ambiguous cases
- [ ] AC-006.3: User can mark two listings as same vehicle
- [ ] AC-006.4: User can mark two listings as different vehicles
- [ ] AC-006.5: Resolution decisions persisted and used for future matching

---

## REQ-DD-007: New Listing Identification

### Formal Statement

The system SHALL accurately identify and flag listings that are genuinely new to the system.

### Acceptance Criteria

- [ ] AC-007.1: Listing is "new" if no existing record matches any deduplication criteria
- [ ] AC-007.2: NewListingFlag set to true for new listings in current search session
- [ ] AC-007.3: New listings highlighted in output display
- [ ] AC-007.4: New listing count tracked per search session
- [ ] AC-007.5: First observation timestamp recorded for new listings

---

## REQ-DD-008: Relisted Vehicle Detection

### Formal Statement

The system SHALL detect when a previously inactive listing reappears.

### Acceptance Criteria

- [ ] AC-008.1: Reappearing listings matched to historical records
- [ ] AC-008.2: Relisted vehicles flagged with RelistedFlag
- [ ] AC-008.3: Time off market calculated and recorded
- [ ] AC-008.4: Price difference from previous listing highlighted
- [ ] AC-008.5: Relisting pattern tracking (frequent relisters flagged)

---

## REQ-DD-009: Dealer Inventory Tracking

### Formal Statement

The system SHALL track vehicles by dealer to improve deduplication accuracy.

### Acceptance Criteria

- [ ] AC-009.1: Dealer name normalized and stored
- [ ] AC-009.2: Same vehicle from same dealer across sites linked
- [ ] AC-009.3: Dealer inventory history trackable
- [ ] AC-009.4: Dealer-specific duplicate resolution rules supported
- [ ] AC-009.5: Dealer reliability metrics calculable from listing patterns

---

## REQ-DD-010: Deduplication Performance

### Formal Statement

The system SHALL perform deduplication efficiently without significantly impacting search performance.

### Acceptance Criteria

- [ ] AC-010.1: VIN lookup uses indexed query (sub-100ms response)
- [ ] AC-010.2: Fuzzy matching optimized with blocking/bucketing strategy
- [ ] AC-010.3: Image matching performed asynchronously if enabled
- [ ] AC-010.4: Deduplication adds no more than 20% overhead to search time
- [ ] AC-010.5: Batch deduplication supported for bulk imports

---

## REQ-DD-011: Deduplication Audit

### Formal Statement

The system SHALL maintain an audit trail of deduplication decisions.

### Acceptance Criteria

- [ ] AC-011.1: Each deduplication decision logged with timestamp and reason
- [ ] AC-011.2: Automatic vs manual decisions distinguished
- [ ] AC-011.3: Audit log queryable for analysis
- [ ] AC-011.4: False positive rate trackable from manual overrides
- [ ] AC-011.5: Deduplication accuracy metrics reportable

---

## REQ-DD-012: Deduplication Configuration

### Formal Statement

The system SHALL provide configurable deduplication settings.

### Acceptance Criteria

- [ ] AC-012.1: Confidence threshold configurable (e.g., `--dedup-threshold 90`)
- [ ] AC-012.2: Individual matching criteria weights configurable
- [ ] AC-012.3: Deduplication can be disabled (e.g., `--no-dedup`)
- [ ] AC-012.4: Strict mode available (VIN-only matching)
- [ ] AC-012.5: Configuration persisted in config file
