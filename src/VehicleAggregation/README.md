# Vehicle Aggregation Service

The Vehicle Aggregation Service aggregates vehicle information from multiple listings, providing unified vehicle records with price analytics.

## Purpose

This microservice is responsible for:
- Aggregating listings into unified vehicle records
- Calculating price statistics across listings
- Managing listing-to-vehicle associations
- Providing price comparison data
- VIN-based vehicle tracking

## Architecture

```
VehicleAggregation/
├── VehicleAggregation.Api/           # REST API layer
│   └── Controllers/
│       └── VehiclesController.cs
├── VehicleAggregation.Core/          # Domain layer
│   ├── Models/
│   │   ├── Vehicle.cs
│   │   └── VehicleListing.cs
│   └── Events/
└── VehicleAggregation.Infrastructure/ # Data access layer
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/vehicles/{id}` | Get vehicle by ID |
| GET | `/api/vehicles/vin/{vin}` | Get vehicle by VIN |
| POST | `/api/vehicles/search` | Search vehicles |
| POST | `/api/vehicles` | Create vehicle record |
| POST | `/api/vehicles/{id}/listings` | Link listing to vehicle |
| DELETE | `/api/vehicles/{id}/listings/{listingId}` | Unlink listing |
| GET | `/api/vehicles/{id}/price-comparison` | Get price analytics |

## Domain Models

### Vehicle
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Vehicle identifier |
| `VIN` | string | Vehicle Identification Number |
| `Make` | string | Manufacturer |
| `Model` | string | Model name |
| `Year` | int | Model year |
| `Trim` | string | Trim level |
| `BodyStyle` | string | Body type |
| `Transmission` | string | Transmission type |
| `Drivetrain` | string | Drive configuration |
| `FuelType` | string | Fuel type |
| `ExteriorColor` | string | Exterior color |
| `InteriorColor` | string | Interior color |
| `BestPrice` | decimal | Lowest current price |
| `AveragePrice` | decimal | Average price |
| `LowestPrice` | decimal | Historical low |
| `HighestPrice` | decimal | Historical high |
| `ListingCount` | int | Active listing count |
| `BestPriceListingId` | Guid | Best deal listing |
| `Listings` | List | Associated listings |

### VehicleListing
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Link identifier |
| `ListingId` | Guid | Listing reference |
| `Price` | decimal | Listing price |
| `Source` | string | Marketplace source |
| `DealerName` | string | Dealer name |
| `AddedAt` | DateTime | Link creation time |

## Features

### VIN-Based Aggregation
- Unique vehicle identification
- Cross-source matching
- Duplicate consolidation
- Vehicle history tracking

### Price Analytics
| Metric | Description |
|--------|-------------|
| `BestPrice` | Lowest current listing price |
| `AveragePrice` | Mean of all listing prices |
| `LowestPrice` | Historical minimum |
| `HighestPrice` | Historical maximum |
| `PriceRange` | High - Low spread |
| `StandardDeviation` | Price variation |

### Price Comparison
Returns comparison data:
- All listings with prices
- Price ranking
- Deal score
- Market position

## Integration Events

| Event | Description |
|-------|-------------|
| `VehicleCreatedEvent` | Published when vehicle created |
| `VehiclePriceUpdatedEvent` | Published on price change |
| `ListingLinkedEvent` | Published when listing linked |

## Integration Points

### Inbound
- **Listing Service**: Receives new listings
- **Deduplication Service**: Receives duplicate matches

### Outbound
- **Search Service**: Provides vehicle data
- **Dashboard Service**: Reports vehicle metrics
- **Alert Service**: Triggers price alerts

## Aggregation Logic

1. New listing received
2. Extract VIN (if available)
3. Find or create vehicle record
4. Link listing to vehicle
5. Recalculate price statistics
6. Update best deal reference

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `vehicle-aggregation.puml` - Aggregation flow
- `price-comparison.puml` - Price analytics flow
