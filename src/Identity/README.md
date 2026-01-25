# Identity Service

The Identity Service handles user authentication, authorization, and API key management for the Automated Market Intelligence Tool platform.

## Purpose

This microservice is responsible for:
- User registration and authentication
- JWT token generation and validation
- Password management (hashing, reset, recovery)
- Email verification
- API key generation and management
- Role-based access control

## Architecture

```
Identity/
├── Identity.Api/           # REST API layer
│   └── Controllers/
│       ├── AuthController.cs
│       ├── ProfileController.cs
│       └── ApiKeysController.cs
├── Identity.Core/          # Domain layer
│   ├── Models/
│   │   ├── UserAggregate/
│   │   ├── ApiKeyAggregate/
│   │   └── RoleAggregate/
│   └── Services/
└── Identity.Infrastructure/ # Data access layer
    └── Data/
        └── IdentityDbContext.cs
```

## API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register a new user |
| POST | `/api/auth/login` | Authenticate user and get tokens |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/verify-email` | Verify user email address |
| POST | `/api/auth/forgot-password` | Initiate password recovery |
| POST | `/api/auth/reset-password` | Reset password with token |
| POST | `/api/auth/logout` | Logout and invalidate tokens |

### Profile
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/profile` | Get current user profile |
| PUT | `/api/profile` | Update user profile |

### API Keys
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/apikeys` | List user's API keys |
| POST | `/api/apikeys` | Generate new API key |
| DELETE | `/api/apikeys/{id}` | Revoke API key |

## Domain Models

### User Aggregate
- **User**: Core user entity with email, password hash, verification status
- **UserProfile**: Extended user information
- **RefreshToken**: JWT refresh token management
- **EmailVerificationToken**: Email verification tracking
- **PasswordResetToken**: Password recovery tokens

### ApiKey Aggregate
- **ApiKey**: API key with hash, prefix, scopes, and usage tracking

### Role Aggregate
- **Role**: User roles and permissions

## Integration Events

| Event | Description |
|-------|-------------|
| `UserRegisteredEvent` | Published when a new user registers |
| `UserEmailVerifiedEvent` | Published when email is verified |
| `UserProfileUpdatedEvent` | Published when profile is updated |
| `UserLoggedInEvent` | Published on successful login |
| `ApiKeyGeneratedEvent` | Published when API key is created |

## Dependencies

- **Outbound**: Email Service (for sending verification emails)
- **Inbound**: All services validate JWT tokens against this service

## Configuration

| Setting | Description |
|---------|-------------|
| `Jwt:Secret` | Secret key for JWT signing |
| `Jwt:Issuer` | Token issuer |
| `Jwt:Audience` | Token audience |
| `Jwt:ExpirationMinutes` | Access token expiration |
| `Jwt:RefreshExpirationDays` | Refresh token expiration |

## Sequence Diagrams

See the `docs/` folder for detailed sequence diagrams:
- `user-registration.puml` - User registration flow
- `user-login.puml` - Authentication flow
- `api-key-generation.puml` - API key creation flow
