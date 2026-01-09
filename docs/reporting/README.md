# Phase 5: Reporting Feature

## Overview

The Phase 5 Reporting feature provides comprehensive report generation capabilities for the Automated Market Intelligence Tool. Users can generate professional-quality reports in multiple formats (HTML, PDF, Excel) with customizable content including market statistics, listing data, and search criteria.

## Features

### Report Formats

- **HTML Reports**: Responsive, print-friendly web pages
- **PDF Reports**: Professional PDF documents with QuestPDF
- **Excel Reports**: Multi-sheet workbooks with ClosedXML

### Report Content

Each report includes:
- Report title and generation timestamp
- Search criteria (applied filters)
- Market statistics (total, averages, distributions)
- Detailed listing data

## Usage

```bash
# Generate HTML report
car-search report -t <tenant-id> -f html -o report.html -m Toyota

# Generate PDF report with filters
car-search report -t <tenant-id> -f pdf --price-max 30000 --year-min 2020

# Generate Excel report
car-search report -t <tenant-id> -f excel -o market-report.xlsx
```

## Dependencies

- **Scriban** (5.10.0): HTML template engine
- **QuestPDF** (2024.12.3): PDF generation
- **ClosedXML** (0.104.2): Excel file generation

## Testing

38 test cases covering 80%+ code coverage across all report generators and services.

For more details, see the full documentation at `docs/specs/reporting/reporting.specs.md`.
