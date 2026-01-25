# Notification Service

The Notification Service provides centralized notification delivery supporting multiple channels and template-based messaging.

## Purpose

This microservice is responsible for:
- Sending notifications via multiple channels
- Managing notification templates
- Webhook integration and delivery
- Tracking notification delivery status
- Retry handling for failed deliveries

## Architecture

```
Notification/
├── Notification.Api/           # REST API layer
│   └── Controllers/
│       └── NotificationsController.cs
├── Notification.Core/          # Domain layer
│   ├── Models/
│   │   ├── NotificationLog.cs
│   │   ├── NotificationTemplate.cs
│   │   └── Webhook.cs
│   └── Services/
│       └── INotificationService.cs
└── Notification.Infrastructure/ # Data access layer
```

## API Endpoints

### Notifications
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/notifications/send` | Send template notification |
| POST | `/api/notifications/send-direct` | Send direct notification |
| POST | `/api/notifications/{id}/retry` | Retry failed notification |
| GET | `/api/notifications/logs` | Get notification logs |

### Templates
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/notifications/templates` | List all templates |
| GET | `/api/notifications/templates/{id}` | Get template by ID |
| POST | `/api/notifications/templates` | Create template |
| PUT | `/api/notifications/templates/{id}` | Update template |
| DELETE | `/api/notifications/templates/{id}` | Delete template |

### Webhooks
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/notifications/webhooks` | List webhooks |
| GET | `/api/notifications/webhooks/{id}` | Get webhook by ID |
| POST | `/api/notifications/webhooks` | Create webhook |
| PUT | `/api/notifications/webhooks/{id}` | Update webhook |
| DELETE | `/api/notifications/webhooks/{id}` | Delete webhook |
| POST | `/api/notifications/webhooks/trigger` | Trigger webhooks |

## Domain Models

### NotificationLog
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Log identifier |
| `RecipientId` | Guid | Recipient user ID |
| `TemplateId` | Guid | Template used |
| `Recipient` | string | Email/endpoint |
| `Subject` | string | Notification subject |
| `Body` | string | Notification content |
| `Status` | enum | Pending, Sent, Failed |
| `SentAt` | DateTime | Delivery time |
| `ErrorMessage` | string | Error details |
| `CreatedAt` | DateTime | Creation time |
| `RetryCount` | int | Retry attempts |

### NotificationTemplate
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Template identifier |
| `Name` | string | Template name |
| `Subject` | string | Subject template |
| `Body` | string | Body template |
| `Type` | enum | Email, SMS, Push |
| `IsActive` | bool | Active status |
| `CreatedAt` | DateTime | Creation time |
| `UpdatedAt` | DateTime | Last update |

### Webhook
| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Webhook identifier |
| `Name` | string | Webhook name |
| `Url` | string | Endpoint URL |
| `Secret` | string | Signing secret |
| `Events` | List | Subscribed events |
| `IsActive` | bool | Active status |
| `Description` | string | Description |
| `FailureCount` | int | Consecutive failures |
| `LastTriggeredAt` | DateTime | Last trigger time |

## Notification Types

| Type | Channel | Description |
|------|---------|-------------|
| Email | SMTP | Email notifications |
| SMS | SMS Gateway | Text messages |
| Push | Push Service | Mobile push |
| Webhook | HTTP POST | External webhooks |

## Template Variables

Templates support variable substitution:
```
Subject: Price Alert: {{Make}} {{Model}}
Body: The {{Year}} {{Make}} {{Model}} you're watching
      dropped from ${{OldPrice}} to ${{NewPrice}}!
```

## Integration Points

### Inbound
- **Alert Service**: Sends alert notifications
- **WatchList Service**: Sends price change alerts
- **Reporting Service**: Sends report deliveries
- **Identity Service**: Sends verification emails

### Outbound
- **Email Provider**: SMTP/SendGrid/etc.
- **SMS Provider**: Twilio/etc.
- **Push Service**: Firebase/etc.

## Retry Logic

| Attempt | Delay | Notes |
|---------|-------|-------|
| 1 | Immediate | First attempt |
| 2 | 1 minute | First retry |
| 3 | 5 minutes | Second retry |
| 4 | 15 minutes | Third retry |
| 5 | 1 hour | Final retry |

After 5 failures, notification is marked as failed.

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `notification-delivery.puml` - Notification sending flow
- `webhook-trigger.puml` - Webhook delivery flow
