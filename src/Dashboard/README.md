# Dashboard Service

The Dashboard Service provides analytics, metrics aggregation, and system health monitoring for the platform.

## Purpose

This microservice is responsible for:
- Aggregating platform metrics
- Providing market trend analysis
- Monitoring scraping operations
- Reporting system health status
- Real-time data visualization support

## Architecture

```
Dashboard/
├── Dashboard.Api/           # REST API layer
│   └── Controllers/
│       └── DashboardController.cs
├── Dashboard.Core/          # Domain layer
│   └── Models/
│       ├── DashboardOverview.cs
│       ├── MarketTrends.cs
│       └── ScrapingStatus.cs
└── Dashboard.Infrastructure/ # Data access layer
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/dashboard/overview` | Get platform overview |
| GET | `/api/dashboard/market-trends` | Get market analysis |
| GET | `/api/dashboard/scraping-status` | Get scraping status |
| GET | `/api/dashboard/health` | Get service health |

## Domain Models

### DashboardOverview
| Field | Type | Description |
|-------|------|-------------|
| `TotalListings` | int | Total listing count |
| `ActiveListings` | int | Active listing count |
| `TotalVehicles` | int | Aggregated vehicles |
| `ActiveAlerts` | int | Active user alerts |
| `PendingReviews` | int | Dedup review queue |
| `NewListingsToday` | int | Today's new listings |
| `PriceChangesToday` | int | Today's price changes |
| `ListingsRemovedToday` | int | Deactivated today |
| `GeneratedAt` | DateTime | Data timestamp |

### MarketTrends
| Field | Type | Description |
|-------|------|-------------|
| `AveragePrice` | decimal | Market average price |
| `MedianPrice` | decimal | Market median price |
| `PriceChangePercent` | decimal | Price trend |
| `ListingsBySource` | Dictionary | Source breakdown |
| `ListingsByMake` | Dictionary | Make breakdown |
| `ListingsByProvince` | Dictionary | Province breakdown |
| `AveragePriceByMake` | Dictionary | Price by make |
| `AverageMileage` | decimal | Average mileage |
| `MostCommonYear` | int | Most frequent year |
| `CalculatedAt` | DateTime | Calculation time |

### ScrapingStatus
| Field | Type | Description |
|-------|------|-------------|
| `ActiveJobs` | int | Running jobs |
| `CompletedToday` | int | Jobs completed today |
| `FailedToday` | int | Failed jobs today |
| `NextScheduledJob` | DateTime | Next scheduled run |
| `ListingsScrapedToday` | int | Listings scraped |
| `SourceStatuses` | List | Per-source status |
| `LastSuccessfulScrape` | DateTime | Last success time |
| `RetrievedAt` | DateTime | Data timestamp |

### SourceStatus
| Field | Type | Description |
|-------|------|-------------|
| `Source` | string | Source name |
| `Status` | enum | Healthy, Degraded, Down |
| `ListingsScraped` | int | Today's count |
| `ErrorRate` | decimal | Error percentage |
| `LastScrape` | DateTime | Last scrape time |

## Features

### Platform Overview
Real-time metrics including:
- Listing counts (total, active, new)
- Vehicle aggregation stats
- User activity metrics
- System health indicators

### Market Trends
Analysis including:
- Price trends over time
- Regional comparisons
- Make/model popularity
- Inventory aging

### Scraping Monitoring
Track scraping operations:
- Job success/failure rates
- Source health status
- Throughput metrics
- Error patterns

## Integration Points

### Inbound
- **Listing Service**: Listing metrics
- **Scraping Orchestration**: Job status
- **Deduplication Service**: Review queue
- **Alert Service**: Alert counts
- **All Services**: Health checks

### Outbound
- **UI/Frontend**: Dashboard displays
- **Reporting Service**: Report data

## Data Refresh

| Metric | Refresh Rate |
|--------|--------------|
| Overview | Real-time |
| Market Trends | 15 minutes |
| Scraping Status | 1 minute |
| Health | 30 seconds |

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `metrics-aggregation.puml` - Metrics collection flow
- `health-monitoring.puml` - Health check flow
