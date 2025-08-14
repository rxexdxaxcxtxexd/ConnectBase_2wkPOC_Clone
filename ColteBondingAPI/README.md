# Colt eBonding API - ASP.NET Core Implementation

## Overview

Enterprise-grade ASP.NET Core 8.0 Web API implementation for Colt eBonding integration, following best practices and industry standards.

## Architecture

### Project Structure
```
ColteBondingAPI/
├── Controllers/          # API endpoints
├── Services/            # Business logic
│   ├── Interfaces/
│   └── Implementations/
├── Infrastructure/      # External integrations
│   ├── Clients/        # HTTP clients
│   └── Authentication/ # Auth services
├── Models/             # DTOs and entities
│   ├── Requests/
│   └── Responses/
├── Middleware/         # Custom middleware
├── Extensions/         # Service extensions
└── Configuration/      # App settings
```

## Features

### Core Functionality
- ✅ Service availability checking
- ✅ Quote management (CRUD operations)
- ✅ Order processing and tracking
- ✅ Service management and modifications
- ✅ Fault management and ticketing
- ✅ Usage reporting and billing
- ✅ Webhook integration

### Technical Features
- ✅ OAuth 2.0 authentication with token caching
- ✅ JWT bearer token authorization
- ✅ Rate limiting per client
- ✅ Circuit breaker pattern for resilience
- ✅ Retry policies with exponential backoff
- ✅ Distributed caching (Redis/Memory)
- ✅ Comprehensive error handling
- ✅ Structured logging with Serilog
- ✅ API versioning
- ✅ OpenAPI/Swagger documentation
- ✅ Health checks
- ✅ Request/Response validation
- ✅ Dependency injection
- ✅ Async/await throughout

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- SQL Server (LocalDB or full instance)
- Redis (optional, for distributed caching)

### Installation

1. Clone the repository
```bash
git clone https://github.com/your-org/colt-ebonding-api.git
cd ColteBondingAPI
```

2. Restore packages
```bash
dotnet restore
```

3. Update configuration
Edit `appsettings.json` with your Colt API credentials:
```json
{
  "ColtApi": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

4. Run the application
```bash
dotnet run
```

5. Access Swagger UI
Navigate to: `https://localhost:5001/swagger`

## Configuration

### Environment Variables
```bash
# Colt API Settings
COLTAPI__CLIENTID=your-client-id
COLTAPI__CLIENTSECRET=your-client-secret
COLTAPI__BASEURL=https://api.colt.net/ebonding/v5

# JWT Settings
AUTHENTICATION__JWT__SECRETKEY=your-secret-key

# Redis Connection
CONNECTIONSTRINGS__REDIS=localhost:6379
```

### appsettings.json Structure
```json
{
  "ColtApi": {
    "BaseUrl": "https://api.colt.net/ebonding/v5",
    "TokenUrl": "https://api.colt.net/auth/token",
    "ClientId": "",
    "ClientSecret": "",
    "Timeout": 30,
    "RetryCount": 3,
    "CircuitBreakerThreshold": 5
  },
  "Authentication": {
    "Jwt": {
      "Issuer": "ColteBondingAPI",
      "Audience": "ColteBondingAPI",
      "SecretKey": "",
      "ExpirationMinutes": 60
    }
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "Window": "00:01:00"
  }
}
```

## API Endpoints

### Availability
- `POST /api/v1/availability/check` - Check service availability
- `GET /api/v1/availability/{locationId}` - Get location availability
- `POST /api/v1/availability/batch` - Batch availability check

### Quotes
- `POST /api/v1/quote` - Create quote
- `GET /api/v1/quote/{quoteId}` - Get quote
- `PUT /api/v1/quote/{quoteId}` - Update quote
- `DELETE /api/v1/quote/{quoteId}` - Delete quote
- `GET /api/v1/quote` - List quotes

### Orders
- `POST /api/v1/order` - Submit order
- `GET /api/v1/order/{orderId}` - Get order
- `GET /api/v1/order/{orderId}/status` - Get order status
- `POST /api/v1/order/{orderId}/cancel` - Cancel order
- `PUT /api/v1/order/{orderId}/modify` - Modify order

## Security

### Authentication
The API uses JWT bearer tokens for authentication:
```http
Authorization: Bearer {token}
```

### Rate Limiting
- Default: 100 requests per minute per client
- Configurable per endpoint
- Returns 429 status with Retry-After header

### Best Practices
- All sensitive data encrypted in transit (TLS 1.2+)
- Credentials stored in secure configuration
- Input validation on all endpoints
- SQL injection prevention
- XSS protection
- CORS configured appropriately

## Error Handling

### Standard Error Response
```json
{
  "code": "ERROR_CODE",
  "message": "Human-readable error message",
  "details": {},
  "traceId": "correlation-id",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### HTTP Status Codes
- `200 OK` - Success
- `201 Created` - Resource created
- `400 Bad Request` - Validation error
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource conflict
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error
- `502 Bad Gateway` - External service error

## Monitoring

### Health Checks
```http
GET /health
```

### Metrics
- Request count and duration
- Error rates
- Cache hit rates
- External API response times
- Circuit breaker state

### Logging
Structured logging with Serilog:
- Console output
- Rolling file logs
- Configurable log levels
- Correlation IDs for request tracking

## Testing

### Unit Tests
```bash
dotnet test ColteBondingAPI.Tests
```

### Integration Tests
```bash
dotnet test ColteBondingAPI.IntegrationTests
```

### Load Testing
Using Apache Bench:
```bash
ab -n 1000 -c 10 -H "Authorization: Bearer {token}" https://localhost:5001/api/v1/availability/check
```

## Deployment

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 80
ENTRYPOINT ["dotnet", "ColteBondingAPI.dll"]
```

### Azure App Service
```bash
az webapp up --name colt-ebonding-api --resource-group rg-colt --plan asp-colt
```

### IIS
1. Install .NET Core Hosting Bundle
2. Create application pool
3. Deploy to wwwroot
4. Configure environment variables

## Performance Optimization

- Response caching for static data
- Redis distributed cache for session data
- Connection pooling for database
- Async operations throughout
- Minimal allocations in hot paths
- Response compression enabled

## Troubleshooting

### Common Issues

1. **Token expiration**
   - Check token refresh logic
   - Verify clock synchronization

2. **Circuit breaker open**
   - Check external service health
   - Review circuit breaker thresholds

3. **Rate limiting**
   - Implement backoff strategy
   - Request limit increase

## Contributing

1. Fork the repository
2. Create feature branch
3. Commit changes
4. Push to branch
5. Create Pull Request

## License

Copyright © 2024 - All rights reserved

## Support

For issues and questions:
- Email: api-support@example.com
- Documentation: https://docs.example.com
- Issue Tracker: https://github.com/your-org/colt-ebonding-api/issues