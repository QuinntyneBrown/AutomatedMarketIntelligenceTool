# WatchList Service

The WatchList Service allows users to maintain personalized lists of watched vehicles with price change tracking.

## Purpose

This microservice is responsible for:
- Managing user watchlists
- Tracking price changes on watched listings
- Storing user notes per listing
- Providing price comparison history
- Notifying users of significant changes

## Architecture

```
WatchList/
├── WatchList.Api/           # REST API layer
│   └── Controllers/
│       └── WatchListController.cs
├── WatchList.Core/          # Domain layer
│   ├── Models/
│   │   └── WatchedListing.cs
│   └── Events/
└── WatchList.Infrastructure/ # Data access layer
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/watchlist/user/{userId}` | Get user's watched listings |
| GET | `/api/watchlist/user/{userId}/listing/{listingId}` | Get specific watched item |
| POST | `/api/watchlist` | Add listing to watchlist |
| DELETE | `/api/watchlist/user/{userId}/listing/{listingId}` | Remove from watchlist |
| PATCH | `/api/watchlist/user/{userId}/listing/{listingId}/notes` | Update notes |
| GET | `/api/watchlist/user/{userId}/changes` | Get price changes |
| POST | `/api/watchlist/listing/{listingId}/price` | Update listing price |

## Domain Model

### WatchedListing
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Record identifier |
| `UserId` | Guid | User who is watching |
| `ListingId` | Guid | Watched listing |
| `Notes` | string | User notes |
| `PriceAtWatch` | decimal | Price when added |
| `CurrentPrice` | decimal | Current price |
| `PriceChange` | decimal | Price difference |
| `AddedAt` | DateTime | When added to watchlist |
| `LastCheckedAt` | DateTime | Last price check |

## Features

### Price Tracking
- Records price at time of watch
- Updates current price automatically
- Calculates price change (amount and percentage)
- Tracks price history

### User Notes
- Personal notes per watched listing
- Notes are private to user
- Support for markdown formatting

### Price Change Notifications
- Significant price drops
- Price increases
- Listing removed from market

## Integration Events

| Event | Description |
|-------|-------------|
| `ListingWatchedEvent` | Published when listing added |
| `ListingUnwatchedEvent` | Published when removed |
| `WatchedPriceChangedEvent` | Published on price change |

## Integration Points

### Inbound
- **Listing Service**: Receives price updates
- **API Gateway**: Routes watchlist requests

### Outbound
- **Notification Service**: Sends price change alerts
- **Dashboard Service**: Reports watchlist metrics

## Use Cases

### Add to Watchlist
1. User finds interesting listing
2. Adds to watchlist with optional notes
3. System records current price
4. User receives future price updates

### Track Price Changes
1. Listing price changes in source
2. Listing Service publishes event
3. WatchList updates current price
4. If significant change, notify user

### View Watchlist
1. User requests watchlist
2. Returns all watched items with:
   - Current prices
   - Price changes
   - User notes
   - Days watched

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `watchlist-management.puml` - Add/remove operations
- `price-tracking.puml` - Price change flow
