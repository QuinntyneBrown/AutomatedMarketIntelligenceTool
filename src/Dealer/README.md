# Dealer Service

The Dealer Service manages dealer information, inventory tracking, and reliability scoring for vehicle dealerships.

## Purpose

This microservice is responsible for:
- Managing dealer profiles and contact information
- Tracking dealer inventory counts
- Calculating and updating reliability scores
- Providing geographic dealer searches
- Normalizing dealer names across sources

## Architecture

```
Dealer/
├── Dealer.Api/              # REST API layer
│   └── Controllers/
│       └── DealersController.cs
├── Dealer.Core/             # Domain layer
│   └── Models/
│       └── Dealer.cs
└── Dealer.Infrastructure/   # Data access layer
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/dealers/{id}` | Get dealer by ID |
| GET | `/api/dealers/normalized/{name}` | Get by normalized name |
| POST | `/api/dealers/search` | Search dealers |
| POST | `/api/dealers` | Create dealer |
| PUT | `/api/dealers/{id}` | Update dealer |
| DELETE | `/api/dealers/{id}` | Delete dealer |
| GET | `/api/dealers/{id}/inventory` | Get inventory count |
| GET | `/api/dealers/{id}/reliability` | Get reliability score |
| PUT | `/api/dealers/{id}/reliability` | Update reliability |
| GET | `/api/dealers/province/{province}` | Get by province |
| GET | `/api/dealers/city/{city}` | Get by city |

## Domain Model

### Dealer
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Dealer identifier |
| `Name` | string | Dealer name |
| `NormalizedName` | string | Standardized name |
| `Website` | string | Dealer website |
| `Phone` | string | Contact phone |
| `Address` | string | Street address |
| `City` | string | City |
| `Province` | string | Province |
| `PostalCode` | string | Postal code |
| `ReliabilityScore` | decimal | Score (0-100) |
| `ListingCount` | int | Active listings |
| `CreatedAt` | DateTime | Record creation |
| `UpdatedAt` | DateTime | Last update |

## Features

### Name Normalization
- Removes common suffixes (Inc, Ltd, etc.)
- Standardizes abbreviations
- Case-insensitive matching
- Handles multiple source names

### Reliability Scoring
Factors considered:
- Listing accuracy
- Price consistency
- Response time
- Customer reviews
- Listing freshness

| Score Range | Rating |
|-------------|--------|
| 90-100 | Excellent |
| 75-89 | Good |
| 60-74 | Average |
| Below 60 | Below Average |

### Geographic Search
- Search by province
- Search by city
- Search within radius
- Postal code lookup

## Integration Points

### Inbound
- **Listing Service**: Links listings to dealers
- **Scraping Worker**: Creates/updates dealers

### Outbound
- **Search Service**: Provides dealer data for searches
- **Dashboard Service**: Reports dealer metrics

## Use Cases

### Dealer Discovery
1. Scraper finds new dealer in listing
2. Check if dealer exists (by normalized name)
3. If new, create dealer record
4. Link listing to dealer

### Reliability Update
1. Periodically calculate scores
2. Analyze listing patterns
3. Update reliability score
4. Flag dealers with low scores

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `dealer-discovery.puml` - Dealer creation flow
- `reliability-scoring.puml` - Score calculation flow
