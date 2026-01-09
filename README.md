# AutomatedMarketIntelligenceTool

[![PR Validation](https://github.com/QuinntyneBrown/AutomatedMarketIntelligenceTool/actions/workflows/pr-validation.yml/badge.svg)](https://github.com/QuinntyneBrown/AutomatedMarketIntelligenceTool/actions/workflows/pr-validation.yml)

A comprehensive market intelligence solution for automated car listing search, web scraping, and analysis across Canadian automotive markets. This tool provides a CLI interface, REST API, and web application for gathering and analyzing automotive market data from Canadian platforms.

## Overview

AutomatedMarketIntelligenceTool is a Canada-focused multi-tier application that enables users to:

- Search and scrape automotive listings from Canadian sources (Autotrader.ca, Kijiji.ca, and more)
- Configure search parameters with postal codes and location-based queries across Canada
- Detect and filter duplicate listings
- Store and persist listing data with multi-tenant support
- Export data in multiple formats (JSON, CSV)
- View real-time scraping statistics and health metrics
- Access data through CLI, REST API, or web interface

**Note:** This tool is specifically designed for the Canadian automotive market, supporting Canadian postal codes, provinces, and local listing platforms.

## Architecture

The project follows Clean Architecture principles with a clear separation of concerns:

### Backend (.NET)

- **AutomatedMarketIntelligenceTool.Core** - Domain models, business logic, and service interfaces
- **AutomatedMarketIntelligenceTool.Infrastructure** - Data access, EF Core implementation, and external service integrations
- **AutomatedMarketIntelligenceTool.Api** - REST API with MediatR CQRS pattern
- **AutomatedMarketIntelligenceTool.Cli** - Command-line interface for automation and scripting

### Frontend (Angular)

- **AutomatedMarketIntelligenceTool.WebApp** - Angular application with Material Design
- Reactive state management using RxJS
- Mobile-first responsive design
- Material 3 design system

## Key Features

### CLI Interface
- Hierarchical command structure with subcommands
- Interactive mode with auto-completion
- Multiple output formats (table, JSON, CSV)
- Progress indicators for long-running operations
- Configurable verbosity levels
- Shell completion support (Bash, Zsh, PowerShell)
- Graceful signal handling

### Web Scraping
- Canadian automotive sites support (Autotrader.ca, Kijiji.ca)
- Multi-browser support (Chromium, Firefox, WebKit)
- Proxy support (HTTP/HTTPS/SOCKS5) with authentication
- User agent rotation for improved scraping reliability
- Rate limiting and retry logic
- Browser automation for dynamic content
- Intelligent duplicate detection
- Real-time progress tracking

### Data Management
- SQL Server Express database
- Multi-tenant architecture with row-level isolation
- Entity Framework Core with migrations
- Structured logging with Serilog
- Data export capabilities

### Configuration
- Search profile management
- Location-based configuration
- Environment-specific settings
- Persistent user preferences

## Technology Stack

### Backend
- .NET 8+
- Entity Framework Core
- MediatR for CQRS
- Serilog for structured logging
- StyleCop & Roslyn analyzers for code quality

### Frontend
- Angular (latest stable)
- Angular Material
- RxJS for reactive programming
- Jest for unit testing
- Playwright for e2e testing

### Database
- SQLite (default, zero-configuration)
- SQL Server Express
- PostgreSQL (Phase 3)

## Getting Started

### Prerequisites
- .NET 8 SDK or later
- Node.js and npm (for frontend)
- SQL Server Express

### Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/AutomatedMarketIntelligenceTool.git
cd AutomatedMarketIntelligenceTool
```

2. Build the backend:
```bash
dotnet restore
dotnet build
```

3. Apply database migrations:
```bash
cd src/AutomatedMarketIntelligenceTool.Infrastructure
dotnet ef database update
```

4. Install frontend dependencies:
```bash
cd src/AutomatedMarketIntelligenceTool.WebApp
npm install
```

### Running the Application

**CLI:**
```bash
cd src/AutomatedMarketIntelligenceTool.Cli
dotnet run -- search --help
```

**API:**
```bash
cd src/AutomatedMarketIntelligenceTool.Api
dotnet run
```

**Web App:**
```bash
cd src/AutomatedMarketIntelligenceTool.WebApp
npm start
```

## Usage Examples

### CLI Commands

Search for cars with Canadian location:
```bash
car-search search --make Toyota --model Camry --postal-code "M5V 3L9" --max-price 30000
```

Search by province and city:
```bash
car-search search --make Honda --city "Toronto" --province ON --radius 50
```

Interactive mode:
```bash
car-search search --interactive
```

List saved listings:
```bash
car-search list --output json
```

Export data:
```bash
car-search export --format csv --output-file results.csv
```

Configure settings:
```bash
car-search config set database.path "C:\Data\cars.db"
```

Check scraper status:
```bash
car-search status --verbose
```

### Phase 3 Advanced Features

Use different browser engines:
```bash
car-search search --browser firefox --make Toyota
car-search search --browser webkit --make Honda
```

Configure proxy for scraping:
```bash
car-search search --proxy http://proxy.example.com:8080 --make Toyota
car-search search --proxy socks5://user:pass@proxy:1080 --make Honda
```

Use PostgreSQL database:
```bash
# Set environment variables
export AMI_DB_PROVIDER=postgresql
export AMI_CONNECTION_STRING="Host=localhost;Database=ami;Username=user;Password=pass"
dotnet ef database update
```

## Project Structure

```
AutomatedMarketIntelligenceTool/
├── src/
│   ├── AutomatedMarketIntelligenceTool.Api/         # REST API
│   ├── AutomatedMarketIntelligenceTool.Application/ # Application layer
│   ├── AutomatedMarketIntelligenceTool.Cli/         # CLI tool
│   ├── AutomatedMarketIntelligenceTool.Core/        # Domain models & services
│   ├── AutomatedMarketIntelligenceTool.Infrastructure/ # Data access & external services
│   └── AutomatedMarketIntelligenceTool.WebApp/      # Angular frontend
├── tests/
│   ├── AutomatedMarketIntelligenceTool.Api.Tests/
│   ├── AutomatedMarketIntelligenceTool.Application.Tests/
│   ├── AutomatedMarketIntelligenceTool.Cli.Tests/
│   ├── AutomatedMarketIntelligenceTool.Core.Tests/
│   └── AutomatedMarketIntelligenceTool.Infrastructure.Tests/
├── docs/
│   └── specs/                                       # Feature specifications
├── eng/                                             # Build & CI/CD scripts
└── infra/                                           # Infrastructure as code

```

## Development

### Coding Standards

The project follows strict architectural and coding standards defined in [docs/specs/implementation-specs.md](docs/specs/implementation-specs.md):

- No Repository pattern - use `IAutomatedMarketIntelligenceToolContext` directly
- Services in Core project (preferred)
- Flattened namespaces matching folder structure
- One class per file
- BEM naming for CSS
- Async pipe pattern for Angular components
- Comprehensive structured logging

### Testing

Run backend tests:
```bash
dotnet test
```

Run backend tests with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

Run frontend unit tests:
```bash
cd src/AutomatedMarketIntelligenceTool.WebApp
npm test
```

Run frontend e2e tests:
```bash
npm run e2e
```

### Configuration

#### Database Provider Configuration

The application supports multiple database providers through environment variables:

**SQLite (Default):**
```bash
# No configuration needed - uses local SQLite database
```

**SQL Server:**
```bash
export AMI_DB_PROVIDER=sqlserver
export AMI_CONNECTION_STRING="Server=localhost;Database=AMI;Trusted_Connection=True;"
```

**PostgreSQL:**
```bash
export AMI_DB_PROVIDER=postgresql
export AMI_CONNECTION_STRING="Host=localhost;Database=ami;Username=user;Password=pass"
```

### Linting

Backend (StyleCop + Roslyn):
```bash
dotnet build
```

Frontend (ESLint):
```bash
npm run lint
```

## Documentation

Detailed specifications for each feature:
- [CLI Interface](docs/specs/cli-interface/cli-interface.specs.md)
- [Web Scraping](docs/specs/web-scraping/web-scraping.specs.md)
- [Duplicate Detection](docs/specs/duplicate-detection/duplicate-detection.specs.md)
- [Data Persistence](docs/specs/data-persistence/data-persistence.specs.md)
- [Location Configuration](docs/specs/location-configuration/location-configuration.specs.md)
- [Search Configuration](docs/specs/search-configuration/search-configuration.specs.md)
- [Reporting](docs/specs/reporting/reporting.specs.md)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Follow the coding standards in the specification documents
4. Ensure all tests pass and linting is clean
5. Submit a pull request

### Branch Protection

The `main` branch is protected and requires:
- All status checks must pass before merging
- PR validation workflow must complete successfully
- Both backend (.NET) and frontend (Angular) builds must succeed
- All unit tests must pass

### Continuous Integration

This project uses GitHub Actions for continuous integration. On every pull request:
- **Backend**: Builds the .NET solution and runs all unit tests
- **Frontend**: Builds the Angular application and runs all unit tests
- **Status**: Check the badge at the top of this README for current build status

To ensure your PR will pass CI checks, run these commands locally before pushing:

**Backend:**
```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

**Frontend:**
```bash
cd src/AutomatedMarketIntelligenceTool.WebApp
npm ci
npm run build
npm test -- --watch=false
```

## License

[Your License Here]

## Support

For issues and questions, please open an issue on GitHub.