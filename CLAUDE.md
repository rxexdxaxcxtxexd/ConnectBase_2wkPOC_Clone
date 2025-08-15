# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

ConnectBase is a telecommunications API integration platform containing multiple .NET applications and comprehensive documentation for various telco providers. The repository includes two main applications:

1. **ColteBondingAPI**: Enterprise ASP.NET Core 8.0 Web API for Colt eBonding integration
2. **TelecomApiAnalyzer**: ASP.NET Core 6.0 web application for analyzing and generating telecom API specifications

## Architecture & Key Components

### ColteBondingAPI Structure
- **Controllers/**: API endpoints (Availability, Order, Quote)
- **Services/**: Business logic with dependency injection pattern
- **Infrastructure/**: External integrations and HTTP clients
- **Models/**: Request/Response DTOs
- **Middleware/**: Exception handling and rate limiting
- **Extensions/**: Service configuration and dependency registration

### TelecomApiAnalyzer Structure
- **Controllers/**: MVC controllers for web interface
- **Services/**: Document analysis, code generation, Postman collection creation
- **Models/**: API documentation models and Lyntia-specific models
- **Views/**: Razor pages with workflow progress tracking

## Common Development Commands

### ColteBondingAPI (.NET 8.0)
```bash
# Build and run
cd ColteBondingAPI
dotnet restore
dotnet build
dotnet run

# Testing
dotnet test ColteBondingAPI.Tests
dotnet test ColteBondingAPI.IntegrationTests

# Access Swagger UI at https://localhost:5001/swagger
```

### TelecomApiAnalyzer (.NET 6.0)
```bash
# Build and run
cd TelecomApiAnalyzer/TelecomApiAnalyzer.Web
dotnet restore
dotnet build
dotnet run

# Access at https://localhost:5001
```

### Solution-wide operations
```bash
# Build entire TelecomApiAnalyzer solution
cd TelecomApiAnalyzer
dotnet build TelecomApiAnalyzer.sln
```

## Configuration Requirements

### ColteBondingAPI Configuration
- **appsettings.json** must include:
  - `ColtApi` section with ClientId, ClientSecret, BaseUrl, TokenUrl
  - `Authentication:Jwt` section with Issuer, Audience, SecretKey
  - `RateLimiting` settings
  - Optional `ConnectionStrings:Redis` for distributed caching

### TelecomApiAnalyzer Configuration
- **appsettings.json** must include:
  - `ApiSettings` section with TokenEndpoint, ClientId, ClientSecret
- Uses session state for workflow management

## Key Features & Patterns

### ColteBondingAPI Enterprise Features
- OAuth 2.0 authentication with token caching
- Rate limiting with configurable policies
- Circuit breaker pattern with Polly
- Structured logging with Serilog
- Health checks at `/health`
- API versioning support
- Comprehensive error handling middleware

### TelecomApiAnalyzer Workflow
1. Upload API documents (PDF, Word, JSON, YAML)
2. Analyze and extract endpoints/models
3. Generate C# client code
4. Configure authentication
5. Generate Postman collections
6. Deploy via Azure Bicep templates

## Deployment

### Azure Deployment (TelecomApiAnalyzer)
Uses Bicep template at `TelecomApiAnalyzer/deploy.bicep`:
```bash
az deployment group create \
  --resource-group your-rg \
  --template-file deploy.bicep \
  --parameters appServiceName=your-app-name
```

### Docker Support (ColteBondingAPI)
- Targets `mcr.microsoft.com/dotnet/aspnet:8.0`
- Exposes port 80
- Requires environment variables for API credentials

## Development Guidelines

- **Target Frameworks**: .NET 8.0 (ColteBondingAPI), .NET 6.0 (TelecomApiAnalyzer)
- **Authentication**: JWT Bearer tokens for APIs, session-based for web apps
- **Logging**: Serilog with structured logging (ColteBondingAPI)
- **Error Handling**: Centralized middleware with standard error responses
- **HTTP Clients**: Configured with retry policies and circuit breakers
- **Caching**: Redis distributed cache support with fallback to in-memory

## Provider Integrations

### Supported APIs
- **Colt eBonding**: Full CRUD operations for availability, quotes, orders
- **Lyntias Quotation API**: Carrier information and quotation creation
- **Generic telco providers**: Extensible architecture for additional integrations

### Documentation Files
- Technical specifications in PDF/Word formats
- Implementation guides for each provider
- Use case documentation for integration patterns