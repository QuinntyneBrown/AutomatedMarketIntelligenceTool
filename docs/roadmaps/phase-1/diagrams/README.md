# Phase 1 Diagrams

This directory contains visual diagrams for the Phase 1 Implementation Roadmap.

## Rendered Diagrams (PNG)

| File | Description |
|------|-------------|
| [architecture-layers.png](./architecture-layers.png) | Clean architecture layer diagram showing project structure and dependencies |
| [implementation-sequence.png](./implementation-sequence.png) | Sprint-by-sprint implementation sequence |
| [dependency-flow.png](./dependency-flow.png) | Data and dependency flow from user request to displayed results |

## Source Files (PlantUML)

| File | Description |
|------|-------------|
| [architecture-layers.puml](./architecture-layers.puml) | PlantUML source for architecture diagram |
| [implementation-sequence.puml](./implementation-sequence.puml) | PlantUML source for sequence diagram |
| [dependency-flow.puml](./dependency-flow.puml) | PlantUML source for dependency flow diagram |

## Draw.io Diagrams

| File | Description |
|------|-------------|
| [implementation-timeline.drawio](./implementation-timeline.drawio) | Interactive timeline diagram (open in draw.io) |

### Opening Draw.io Files

The `.drawio` files can be opened using:
1. **Online**: [app.diagrams.net](https://app.diagrams.net) - Open file from local disk
2. **VS Code**: Install "Draw.io Integration" extension
3. **Desktop**: Download [draw.io Desktop](https://github.com/jgraph/drawio-desktop/releases)

## Regenerating PlantUML Diagrams

To regenerate the PNG files from PlantUML sources:

```bash
# Download PlantUML jar (if not available)
curl -L -o plantuml.jar https://github.com/plantuml/plantuml/releases/download/v1.2024.7/plantuml-1.2024.7.jar

# Generate PNG from all .puml files
java -jar plantuml.jar -tpng *.puml
```

## Diagram Descriptions

### Architecture Layers
Shows the Clean Architecture structure with:
- **Presentation Layer**: CLI, API, WebApp
- **Application Layer**: MediatR CQRS handlers
- **Domain Layer**: Aggregates, services, value objects
- **Infrastructure Layer**: DbContext, scrapers

### Implementation Sequence
Visualizes the build order across 5 sprints:
1. Foundation (Domain models, DbContext)
2. Data Collection (Scrapers)
3. Business Logic (Duplicate detection, Search)
4. User Interface (CLI, formatters)
5. Integration (E2E testing, polish)

### Dependency Flow
Illustrates the end-to-end data flow:
- Search flow: User → CLI → MediatR → Services → DB → Output
- Scrape flow: User → CLI → Scrapers → Dedup → DB

### Implementation Timeline
Interactive draw.io diagram showing:
- Sprint breakdown with task details
- Cross-sprint dependencies
- Key patterns applied
- Build order rationale
