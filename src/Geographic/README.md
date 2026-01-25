# Geographic Service

The Geographic Service handles geographic operations including geocoding, distance calculations, and custom market definitions.

## Purpose

This microservice is responsible for:
- Geocoding addresses to coordinates
- Calculating distances between locations
- Managing custom market areas
- Providing radius-based queries
- Postal code lookups

## Architecture

```
Geographic/
├── Geographic.Api/           # REST API layer
│   └── Controllers/
│       └── MarketsController.cs
├── Geographic.Core/          # Domain layer
│   ├── Models/
│   │   ├── CustomMarket.cs
│   │   ├── GeoLocation.cs
│   │   └── DistanceResult.cs
│   └── Services/
│       └── IGeoDistanceCalculator.cs
└── Geographic.Infrastructure/ # Data access layer
```

## API Endpoints

### Markets
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/markets` | Get all markets |
| GET | `/api/markets/{id}` | Get market by ID |
| GET | `/api/markets/search` | Search by name |
| POST | `/api/markets` | Create market |
| PUT | `/api/markets/{id}` | Update market |
| DELETE | `/api/markets/{id}` | Delete market |

### Geocoding
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/markets/geocode` | Geocode address |
| POST | `/api/markets/distance` | Calculate distance |
| POST | `/api/markets/within` | Check if within radius |

## Domain Models

### CustomMarket
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Market identifier |
| `Name` | string | Market name |
| `CenterLatitude` | decimal | Center latitude |
| `CenterLongitude` | decimal | Center longitude |
| `RadiusKm` | decimal | Radius in kilometers |
| `PostalCodes` | List | Included postal codes |
| `CreatedAt` | DateTime | Creation time |

### GeoLocation
| Field | Type | Description |
|-------|------|-------------|
| `Latitude` | decimal | Latitude coordinate |
| `Longitude` | decimal | Longitude coordinate |
| `City` | string | City name |
| `Province` | string | Province |
| `PostalCode` | string | Postal code |

### DistanceResult
| Field | Type | Description |
|-------|------|-------------|
| `FromLocation` | GeoLocation | Origin point |
| `ToLocation` | GeoLocation | Destination point |
| `DistanceKm` | decimal | Distance in km |

## Core Services

### IGeoDistanceCalculator
Distance calculation using Haversine formula:
- Earth radius: 6,371 km
- Input: Two lat/long pairs
- Output: Distance in kilometers

## Features

### Custom Markets
Define geographic areas for:
- Regional searches
- Market analysis
- Price comparisons by area
- Dealer territories

### Geocoding
Convert addresses to coordinates:
- Full address geocoding
- Postal code lookup
- City/Province resolution
- Reverse geocoding

### Distance Calculations
| Calculation | Description |
|-------------|-------------|
| Point-to-Point | Distance between two locations |
| Within Radius | Check if point is in circle |
| Nearest | Find closest from list |

## Integration Points

### Inbound
- **Search Service**: Location-based searches
- **Listing Service**: Geocode listing addresses

### Outbound
- **External Geocoding API**: Address resolution
- **Postal Code Database**: Postal code lookups

## Use Cases

### Search by Location
1. User enters postal code
2. Geocode to coordinates
3. Find listings within radius
4. Sort by distance

### Market Analysis
1. Define custom market
2. Aggregate listings in market
3. Calculate market statistics
4. Compare to other markets

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `geocoding.puml` - Address geocoding flow
- `distance-calculation.puml` - Distance calculation flow
