# AutomatedMarketIntelligenceTool

A comprehensive market intelligence solution for automated car listing search, web scraping, and analysis. This tool provides a CLI interface, REST API, and web application for gathering and analyzing automotive market data across multiple platforms.

## Overview

AutomatedMarketIntelligenceTool is a multi-tier application that enables users to:

- Search and scrape automotive listings from multiple sources
- Configure search parameters and location-based queries
- Detect and filter duplicate listings
- Store and persist listing data with multi-tenant support
- Export data in multiple formats (JSON, CSV)
- View real-time scraping statistics and health metrics
- Access data through CLI, REST API, or web interface

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
- Multi-site scraping support
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
- SQL Server Express

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

Search for cars:
```bash
car-search search --make Toyota --model Camry --location "New York" --max-price 25000
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

Run frontend unit tests:
```bash
cd src/AutomatedMarketIntelligenceTool.WebApp
npm test
```

Run frontend e2e tests:
```bash
npm run e2e
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

## License

[Your License Here]

## Support

For issues and questions, please open an issue on GitHub.