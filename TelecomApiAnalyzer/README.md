# Telecom API Analyzer

A comprehensive ASP.NET Core application for analyzing telecom API specifications, generating code, and deploying API integrations.

## Features

- **API Document Analysis**: Upload and analyze API specifications (PDF, Word, JSON, YAML)
- **Technical Specification Generation**: Automatically generate technical specifications from API documents
- **Use Case Guide Creation**: Generate comprehensive use case documentation
- **Code Generation**: Automatically generate C# API client code
- **Authentication Management**: Configure and manage API authentication details
- **Postman Collection Generation**: Create Postman collections for API testing
- **Azure Deployment**: Generate Bicep templates for Azure deployment

## Workflow

The application follows a 7-step workflow:

1. **Upload API Document** - Upload your API specification file
2. **Analyze Document** - Extract endpoints, models, and authentication details
3. **Generate Code** - Create API client code in C#
4. **Authentication Details** - Configure authentication settings
5. **Customer Mapping** - Map customer-specific fields (TBD)
6. **Run Tests** - Generate and execute Postman tests
7. **Deploy** - Deploy to Azure using Bicep templates

## Getting Started

### Prerequisites

- .NET 6.0 SDK or later
- Visual Studio 2022 or VS Code
- Azure subscription (for deployment)

### Installation

1. Clone the repository
2. Navigate to the project directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Build the solution:
   ```bash
   dotnet build
   ```
5. Run the application:
   ```bash
   cd TelecomApiAnalyzer.Web
   dotnet run
   ```

### Configuration

Update `appsettings.json` with your API settings:

```json
{
  "ApiSettings": {
    "TokenEndpoint": "https://your-api-manager.com/token",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

## Usage

1. Navigate to the application URL (default: https://localhost:5001)
2. Click "Select File" to upload your API specification
3. Follow the workflow steps to analyze, generate code, and deploy

## Supported API Specifications

Currently supports the Lyntia Quotation API with:
- Carrier information endpoints
- Quotation creation endpoints
- Authentication via Bearer tokens
- Error handling and use cases

## Project Structure

```
TelecomApiAnalyzer/
├── TelecomApiAnalyzer.Web/
│   ├── Controllers/
│   │   └── ApiAnalyzerController.cs
│   ├── Models/
│   │   ├── ApiDocumentModels.cs
│   │   └── LyntiaApiModels.cs
│   ├── Services/
│   │   ├── ApiDocumentAnalyzer.cs
│   │   ├── CodeGenerationService.cs
│   │   └── PostmanCollectionGenerator.cs
│   ├── Views/
│   │   └── ApiAnalyzer/
│   │       ├── Index.cshtml
│   │       └── Analysis.cshtml
│   └── Program.cs
└── deploy.bicep
```

## Deployment

### Azure Deployment

Use the generated Bicep template to deploy to Azure:

```bash
az deployment group create \
  --resource-group your-rg \
  --template-file deploy.bicep \
  --parameters appServiceName=your-app-name
```

## License

MIT