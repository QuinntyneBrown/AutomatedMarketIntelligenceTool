# Search Service

The Search Service provides advanced search capabilities for vehicle listings with faceted search, autocomplete, and saved search profiles.

## Purpose

This microservice is responsible for:
- Executing complex vehicle searches with multiple criteria
- Providing faceted search with aggregations
- Supporting autocomplete suggestions
- Managing saved search profiles per user
- Location-based radius searches

## Architecture

```
Search/
├── Search.Api/              # REST API layer
│   └── Controllers/
│       └── SearchController.cs
├── Search.Core/             # Domain layer
│   ├── Models/
│   │   ├── SearchCriteria.cs
│   │   ├── SearchProfile.cs
│   │   ├── SearchResultItem.cs
│   │   └── SearchFacets.cs
│   └── Services/
│       ├── ISearchService.cs
│       └── ISearchProfileService.cs
└── Search.Infrastructure/   # Data access layer
```

## API Endpoints

### Search
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/search` | Execute search with criteria |
| GET | `/api/search/autocomplete` | Get autocomplete suggestions |
| POST | `/api/search/facets` | Get available facets/filters |

### Search Profiles
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/search/profiles/{id}` | Get search profile |
| GET | `/api/search/profiles/user/{userId}` | Get user's profiles |
| GET | `/api/search/profiles/active` | Get active profiles |
| POST | `/api/search/profiles` | Create profile |
| PUT | `/api/search/profiles/{id}` | Update profile |
| DELETE | `/api/search/profiles/{id}` | Delete profile |
| POST | `/api/search/profiles/{id}/activate` | Activate profile |
| POST | `/api/search/profiles/{id}/deactivate` | Deactivate profile |

## Domain Models

### SearchCriteria
| Field | Type | Description |
|-------|------|-------------|
| `Make` | string | Vehicle manufacturer |
| `Model` | string | Vehicle model |
| `YearFrom/YearTo` | int | Year range |
| `PriceFrom/PriceTo` | decimal | Price range |
| `MileageFrom/MileageTo` | int | Mileage range |
| `BodyStyle` | string | Vehicle body type |
| `Transmission` | string | Transmission type |
| `Drivetrain` | string | Drive configuration |
| `FuelType` | string | Fuel type |
| `Province` | string | Location province |
| `City` | string | Location city |
| `RadiusKm` | int | Search radius |
| `Keywords` | string | Text search |
| `CertifiedPreOwned` | bool | CPO filter |
| `DealerOnly` | bool | Dealer listings only |
| `PrivateSellerOnly` | bool | Private listings only |

### SearchResultItem
| Field | Type | Description |
|-------|------|-------------|
| `VehicleId` | Guid | Vehicle identifier |
| `ListingId` | Guid | Listing identifier |
| `Make` | string | Manufacturer |
| `Model` | string | Model |
| `Year` | int | Model year |
| `Trim` | string | Trim level |
| `Price` | decimal | Listing price |
| `Mileage` | int | Odometer |
| `BodyStyle` | string | Body type |
| `Location` | string | Location |
| `DealerName` | string | Dealer name |
| `ImageUrl` | string | Primary image |
| `Score` | decimal | Relevance score |

### SearchFacets
Aggregated filter options:
- `Makes` - Available manufacturers
- `Models` - Available models
- `Years` - Year range
- `BodyStyles` - Body type counts
- `Transmissions` - Transmission counts
- `Drivetrains` - Drivetrain counts
- `FuelTypes` - Fuel type counts
- `Colors` - Color counts
- `PriceRange` - Min/max prices
- `MileageRange` - Min/max mileage

### SearchProfile
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Profile identifier |
| `Name` | string | Profile name |
| `UserId` | Guid | Owner user ID |
| `Criteria` | SearchCriteria | Saved criteria |
| `IsActive` | bool | Active status |
| `CreatedAt` | DateTime | Creation time |
| `UpdatedAt` | DateTime | Last update |

## Integration Points

### Inbound
- **Listing Service**: Indexes listing data for search
- **API Gateway**: Routes search requests

### Outbound
- **Alert Service**: Active profiles can trigger alerts
- **Geographic Service**: Location-based queries

## Features

### Autocomplete
- Make/Model suggestions
- Recent searches
- Popular searches

### Faceted Search
- Dynamic filter counts
- Real-time aggregations
- Price/mileage ranges

### Saved Profiles
- Multiple profiles per user
- Activate/deactivate profiles
- Profile-based alerts

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `search-execution.puml` - Search execution flow
- `profile-management.puml` - Profile CRUD operations
