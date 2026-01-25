# Alert Service

The Alert Service manages user alerts for vehicle price changes and new listings matching specified criteria.

## Purpose

This microservice is responsible for:
- Creating and managing user alerts
- Matching new listings against alert criteria
- Tracking alert trigger history
- Sending notifications via multiple channels
- Managing alert lifecycle (activate/deactivate)

## Architecture

```
Alert/
├── Alert.Api/               # REST API layer
│   └── Controllers/
│       └── AlertsController.cs
├── Alert.Core/              # Domain layer
│   ├── Models/
│   │   ├── Alert.cs
│   │   └── AlertNotification.cs
│   └── Events/
└── Alert.Infrastructure/    # Data access layer
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/alerts/{id}` | Get alert by ID |
| GET | `/api/alerts/user/{userId}` | Get user's alerts |
| GET | `/api/alerts/active` | Get all active alerts |
| POST | `/api/alerts` | Create new alert |
| PUT | `/api/alerts/{id}` | Update alert |
| DELETE | `/api/alerts/{id}` | Delete alert |
| POST | `/api/alerts/{id}/activate` | Activate alert |
| POST | `/api/alerts/{id}/deactivate` | Deactivate alert |
| POST | `/api/alerts/check` | Check alerts for listing |
| GET | `/api/alerts/{id}/history` | Get trigger history |

## Domain Models

### Alert
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Alert identifier |
| `Name` | string | Alert name |
| `UserId` | Guid | Owner user ID |
| `IsActive` | bool | Active status |
| `Criteria` | AlertCriteria | Match criteria |
| `NotificationMethod` | enum | Email, Webhook, Both |
| `Email` | string | Notification email |
| `WebhookUrl` | string | Webhook endpoint |
| `CreatedAt` | DateTime | Creation time |
| `UpdatedAt` | DateTime | Last update |
| `LastTriggeredAt` | DateTime | Last trigger time |
| `TriggerCount` | int | Total triggers |

### AlertCriteria
| Field | Type | Description |
|-------|------|-------------|
| `Make` | string | Vehicle manufacturer |
| `Model` | string | Vehicle model |
| `YearFrom/YearTo` | int | Year range |
| `MinPrice/MaxPrice` | decimal | Price range |
| `MaxMileage` | int | Maximum mileage |
| `Trim` | string | Trim level |
| `BodyStyle` | string | Body type |
| `Transmission` | string | Transmission type |
| `FuelType` | string | Fuel type |
| `ExteriorColor` | string | Color preference |

### AlertNotification
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Notification ID |
| `AlertId` | Guid | Parent alert |
| `VehicleId` | Guid | Matched vehicle |
| `MatchedPrice` | decimal | Price at match |
| `Message` | string | Notification text |
| `WasSent` | bool | Delivery status |
| `CreatedAt` | DateTime | Creation time |
| `SentAt` | DateTime | Delivery time |

## Notification Methods

| Method | Description |
|--------|-------------|
| `Email` | Send email notification |
| `Webhook` | POST to webhook URL |
| `Both` | Email and webhook |

## Integration Events

| Event | Description |
|-------|-------------|
| `AlertTriggeredEvent` | Published when alert matches |
| `AlertCreatedEvent` | Published when alert created |
| `AlertDeactivatedEvent` | Published when deactivated |

## Integration Points

### Inbound
- **Listing Service**: Receives new listings to check
- **Search Service**: Receives profile-based alerts

### Outbound
- **Notification Service**: Sends alert notifications
- **Dashboard Service**: Reports alert metrics

## Alert Matching Logic

1. New listing received
2. Get all active alerts
3. For each alert, check criteria:
   - Make/Model match
   - Year within range
   - Price within range
   - Mileage below max
   - Other criteria match
4. If matched:
   - Create notification record
   - Send via configured method
   - Update trigger count

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `alert-creation.puml` - Alert creation flow
- `alert-triggering.puml` - Alert matching and notification flow
