# Scraping Worker Service

The Scraping Worker Service performs the actual web scraping operations for vehicle listings from various marketplace sources.

## Purpose

This microservice is responsible for:
- Executing web scraping operations against marketplace websites
- Implementing source-specific scraping logic
- Managing rate limiting to respect source servers
- Rotating user agents to avoid detection
- Handling errors and retries

## Architecture

```
ScrapingWorker/
├── ScrapingWorker.Core/           # Domain layer
│   └── Services/
│       ├── ISiteScraper.cs
│       ├── IScraperFactory.cs
│       ├── IRateLimiter.cs
│       └── IUserAgentService.cs
├── ScrapingWorker.Infrastructure/ # Implementation layer
│   └── Scrapers/
│       ├── KijijiScraper.cs
│       ├── AutoTraderScraper.cs
│       └── ...
└── ScrapingWorker.Worker/         # Background worker
```

## Core Services

### ISiteScraper
Interface for source-specific scrapers with methods:
- `ScrapeAsync(SearchParameters)` - Execute scraping
- `SupportsSource(string source)` - Check source support

### IScraperFactory
Factory for creating appropriate scraper instances based on source.

### IRateLimiter
Rate limiting service to:
- Throttle requests per source
- Implement backoff strategies
- Track request counts

### IUserAgentService
User agent management to:
- Rotate user agents
- Maintain realistic browser fingerprints
- Avoid bot detection

## Supported Sources

| Source | Status | Notes |
|--------|--------|-------|
| Kijiji | Active | Canadian classifieds |
| AutoTrader | Active | Dealer and private listings |
| CarGurus | Planned | Price analysis |
| Facebook Marketplace | Planned | Private seller listings |

## Worker Pattern

This service operates as a background worker:
1. Receives jobs from Scraping Orchestration Service
2. Executes scraping with appropriate scraper
3. Applies rate limiting between requests
4. Publishes scraped listings as events
5. Reports job completion/failure

## Configuration

| Setting | Description |
|---------|-------------|
| `Scraping:RateLimitPerMinute` | Requests per minute per source |
| `Scraping:MaxConcurrentJobs` | Concurrent scraping jobs |
| `Scraping:RetryCount` | Retry attempts on failure |
| `Scraping:RetryDelayMs` | Delay between retries |
| `Scraping:UserAgentRotationInterval` | UA rotation frequency |

## Integration Points

### Inbound
- **Scraping Orchestration Service**: Receives job assignments

### Outbound
- **Listing Service**: Sends scraped listing data via events
- **Image Service**: Provides image URLs for processing

## Error Handling

| Error Type | Handling |
|------------|----------|
| Rate Limited (429) | Exponential backoff |
| Connection Error | Retry with delay |
| Parse Error | Log and continue |
| Blocked | Rotate UA, change proxy |

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `scraping-execution.puml` - Scraping execution flow
- `rate-limiting.puml` - Rate limiting behavior
