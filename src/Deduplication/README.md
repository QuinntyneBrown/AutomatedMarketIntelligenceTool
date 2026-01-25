# Deduplication Service

The Deduplication Service detects duplicate vehicle listings across multiple marketplaces using multi-factor analysis.

## Purpose

This microservice is responsible for:
- Detecting duplicate listings using multiple signals
- Calculating confidence scores for potential matches
- Managing a review queue for uncertain matches
- Tracking deduplication accuracy metrics
- Providing configurable matching thresholds

## Architecture

```
Deduplication/
├── Deduplication.Api/           # REST API layer
│   └── Controllers/
│       ├── DeduplicationController.cs
│       ├── DuplicatesController.cs
│       ├── ReviewsController.cs
│       └── ConfigsController.cs
├── Deduplication.Core/          # Domain layer
│   ├── Models/
│   │   ├── ListingData.cs
│   │   ├── DeduplicationResult.cs
│   │   ├── DuplicateMatch.cs
│   │   ├── ReviewItem.cs
│   │   └── AccuracyMetrics.cs
│   └── Services/
│       ├── IDuplicateDetectionService.cs
│       └── IAccuracyMetricsService.cs
└── Deduplication.Infrastructure/ # Data access layer
```

## API Endpoints

### Deduplication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/deduplication/analyze` | Analyze listings for duplicates |
| GET | `/api/deduplication/metrics` | Get accuracy metrics |

### Duplicates
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/duplicates/{id}` | Get specific match |
| GET | `/api/duplicates/listing/{listingId}` | Get matches for listing |
| GET | `/api/duplicates/confidence/{level}` | Filter by confidence |
| DELETE | `/api/duplicates/{id}` | Delete match record |

### Reviews
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/reviews` | Get review queue |
| POST | `/api/reviews/{id}/confirm` | Confirm as duplicate |
| POST | `/api/reviews/{id}/reject` | Reject as not duplicate |

### Configuration
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/configs` | Get deduplication config |
| PUT | `/api/configs` | Update thresholds |

## Domain Models

### ListingData
Input data for deduplication analysis:
- Title, VIN, Make, Model, Year
- Price, Mileage, ImageHash
- Location (City, Province, PostalCode, Lat/Long)
- Source, SourceListingId

### DuplicateMatch
| Field | Type | Description |
|-------|------|-------------|
| `SourceListingId` | Guid | First listing |
| `TargetListingId` | Guid | Matched listing |
| `OverallScore` | decimal | Combined score (0-100) |
| `Confidence` | enum | High, Medium, Low |
| `ScoreBreakdown` | object | Individual factor scores |
| `DetectedAt` | DateTime | Detection time |
| `IsConfirmed` | bool | Review confirmation |

### ScoreBreakdown
| Factor | Weight | Description |
|--------|--------|-------------|
| `TitleScore` | 15% | Title similarity |
| `VinScore` | 30% | VIN match (exact) |
| `ImageHashScore` | 25% | Image similarity |
| `PriceScore` | 10% | Price proximity |
| `MileageScore` | 10% | Mileage proximity |
| `LocationScore` | 10% | Geographic proximity |

### Confidence Levels
| Level | Score Range | Action |
|-------|-------------|--------|
| High | 85-100 | Auto-confirm |
| Medium | 60-84 | Review required |
| Low | 40-59 | Review recommended |

### AccuracyMetrics
| Metric | Description |
|--------|-------------|
| `TotalMatches` | Total matches found |
| `ConfirmedDuplicates` | Verified duplicates |
| `ConfirmedNotDuplicates` | False positives |
| `PendingReviews` | Awaiting review |
| `Precision` | True positives / All positives |
| `Recall` | True positives / All actual |
| `F1Score` | Harmonic mean of P&R |

## Integration Events

| Event | Description |
|-------|-------------|
| `DuplicateFoundEvent` | Published when duplicate detected |
| `DeduplicationCompletedEvent` | Published when analysis completes |
| `ReviewRequiredEvent` | Published when review needed |

## Integration Points

### Inbound
- **Listing Service**: Receives new listings for analysis
- **Image Service**: Gets image hashes for comparison

### Outbound
- **Dashboard Service**: Reports deduplication metrics
- **Notification Service**: Alerts on review queue items

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `duplicate-detection.puml` - Duplicate detection flow
- `review-workflow.puml` - Manual review process
