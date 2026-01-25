# Listing Service

The Listing Service manages vehicle listings from various marketplace sources, providing CRUD operations and price history tracking.

## Purpose

This microservice is responsible for:
- Managing vehicle listings from multiple sources (Kijiji, AutoTrader, etc.)
- Tracking price changes over time
- Supporting batch operations for bulk imports
- Providing search and filter capabilities
- Managing listing lifecycle (active/inactive status)

## Architecture

```
Listing/
├── Listing.Api/            # REST API layer
│   └── Controllers/
│       └── ListingsController.cs
├── Listing.Core/           # Domain layer
│   ├── Models/
│   │   └── Listing.cs
│   ├── Services/
│   │   └── IListingService.cs
│   └── Events/
└── Listing.Infrastructure/ # Data access layer
    └── Data/
        └── ListingDbContext.cs
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/listings` | Search listings with filters |
| GET | `/api/listings/{id}` | Get listing by ID |
| POST | `/api/listings` | Create new listing |
| PUT | `/api/listings/{id}` | Update listing |
| DELETE | `/api/listings/{id}` | Deactivate listing |
| GET | `/api/listings/{id}/price-history` | Get price history |
| POST | `/api/listings/batch` | Batch create listings |

### Search Parameters
- `Make` - Vehicle manufacturer
- `Model` - Vehicle model
- `YearFrom/YearTo` - Year range
- `PriceFrom/PriceTo` - Price range
- `MileageFrom/MileageTo` - Mileage range
- `Province` - Location filter
- `Source` - Marketplace source

## Domain Model

### Listing Entity
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Unique identifier |
| `SourceListingId` | string | Original ID from source |
| `Source` | string | Marketplace source |
| `Title` | string | Listing title |
| `Price` | decimal | Current price |
| `Make` | string | Vehicle manufacturer |
| `Model` | string | Vehicle model |
| `Year` | int | Model year |
| `Mileage` | int | Odometer reading |
| `VIN` | string | Vehicle identification number |
| `Condition` | string | New/Used |
| `BodyStyle` | string | Sedan, SUV, etc. |
| `Transmission` | string | Auto/Manual |
| `FuelType` | string | Gas/Electric/Hybrid |
| `Drivetrain` | string | FWD/AWD/RWD |
| `ExteriorColor` | string | Exterior color |
| `InteriorColor` | string | Interior color |
| `City` | string | Location city |
| `Province` | string | Location province |
| `PostalCode` | string | Postal code |
| `ListingUrl` | string | Original listing URL |
| `ImageUrls` | List | Image URLs |
| `FirstSeenAt` | DateTime | First scraped date |
| `LastSeenAt` | DateTime | Last seen date |
| `IsActive` | bool | Active status |

## Integration Events

| Event | Description |
|-------|-------------|
| `ListingCreatedEvent` | Published when a new listing is created |
| `ListingUpdatedEvent` | Published when listing is updated |
| `ListingDeletedEvent` | Published when listing is deactivated |
| `PriceChangedEvent` | Published when price changes |

## Integration Points

### Inbound
- **Scraping Orchestration Service**: Sends scraped listings via events
- **API Gateway**: Routes external requests

### Outbound
- **Image Service**: Triggers image processing for new listings
- **Deduplication Service**: Triggers duplicate detection
- **Vehicle Aggregation Service**: Updates vehicle aggregates
- **Alert Service**: Triggers alert checks for new listings
- **Search Service**: Indexes listing data

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `listing-creation.puml` - Listing creation flow
- `price-change.puml` - Price update flow
- `batch-import.puml` - Batch import flow
