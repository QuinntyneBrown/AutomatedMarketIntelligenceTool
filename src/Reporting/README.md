# Reporting Service

The Reporting Service generates detailed reports in multiple formats with scheduling capabilities for automated delivery.

## Purpose

This microservice is responsible for:
- Generating on-demand reports
- Scheduling recurring reports
- Supporting multiple output formats
- Managing report storage and delivery
- Providing report templates

## Architecture

```
Reporting/
├── Reporting.Api/           # REST API layer
│   └── Controllers/
│       └── ReportsController.cs
├── Reporting.Core/          # Domain layer
│   ├── Models/
│   │   ├── Report.cs
│   │   └── ScheduledReport.cs
│   └── Events/
└── Reporting.Infrastructure/ # Data access layer
```

## API Endpoints

### Reports
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/reports` | Generate report |
| GET | `/api/reports/{id}` | Get report status |
| GET | `/api/reports` | List reports |
| GET | `/api/reports/{id}/download` | Download report |
| DELETE | `/api/reports/{id}` | Delete report |

### Scheduled Reports
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/reports/scheduled` | Create schedule |
| GET | `/api/reports/scheduled/{id}` | Get schedule |
| GET | `/api/reports/scheduled` | List schedules |
| PUT | `/api/reports/scheduled/{id}` | Update schedule |
| POST | `/api/reports/scheduled/{id}/activate` | Activate |
| POST | `/api/reports/scheduled/{id}/deactivate` | Deactivate |
| DELETE | `/api/reports/scheduled/{id}` | Delete schedule |

## Domain Models

### Report
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Report identifier |
| `Name` | string | Report name |
| `Type` | enum | Report type |
| `Status` | enum | Pending, Processing, Completed, Failed |
| `Format` | enum | Excel, PDF, HTML, CSV |
| `Parameters` | Dictionary | Report parameters |
| `FilePath` | string | Generated file path |
| `ErrorMessage` | string | Error details |
| `CreatedAt` | DateTime | Request time |
| `CompletedAt` | DateTime | Completion time |
| `RequestedBy` | Guid | Requesting user |

### ScheduledReport
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Schedule identifier |
| `Name` | string | Schedule name |
| `ReportType` | enum | Report type |
| `CronExpression` | string | Cron schedule |
| `Format` | enum | Output format |
| `Parameters` | Dictionary | Report parameters |
| `IsActive` | bool | Active status |
| `LastRunAt` | DateTime | Last execution |
| `NextRunAt` | DateTime | Next execution |
| `OwnerId` | Guid | Schedule owner |

## Report Types

| Type | Description |
|------|-------------|
| `MarketOverview` | Market summary report |
| `PriceTrends` | Price trend analysis |
| `InventoryReport` | Inventory by source/dealer |
| `DuplicateReport` | Duplicate detection summary |
| `AlertSummary` | Alert activity report |
| `UserActivity` | User engagement metrics |
| `ScrapingReport` | Scraping performance |

## Output Formats

| Format | Extension | Description |
|--------|-----------|-------------|
| Excel | .xlsx | Spreadsheet with charts |
| PDF | .pdf | Formatted document |
| HTML | .html | Web-viewable report |
| CSV | .csv | Raw data export |

## Scheduling

CRON expression support:
```
┌───────────── minute (0 - 59)
│ ┌───────────── hour (0 - 23)
│ │ ┌───────────── day of month (1 - 31)
│ │ │ ┌───────────── month (1 - 12)
│ │ │ │ ┌───────────── day of week (0 - 6)
│ │ │ │ │
* * * * *
```

Common schedules:
- `0 8 * * 1` - Every Monday at 8 AM
- `0 0 1 * *` - First of each month
- `0 */6 * * *` - Every 6 hours

## Integration Events

| Event | Description |
|-------|-------------|
| `ReportGeneratedEvent` | Published when complete |
| `ReportFailedEvent` | Published on failure |
| `ScheduledReportTriggeredEvent` | Published on schedule |

## Integration Points

### Inbound
- **Dashboard Service**: Gets metrics data
- **Listing Service**: Gets listing data
- **All Services**: Aggregates data for reports

### Outbound
- **Notification Service**: Report delivery
- **Blob Storage**: Report file storage

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `report-generation.puml` - Report generation flow
- `scheduled-reports.puml` - Scheduled execution flow
