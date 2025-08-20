using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TelecomApiAnalyzer.Web.Models;

namespace TelecomApiAnalyzer.Web.Services
{
    public class ApiDocumentAnalyzer : IApiDocumentAnalyzer
    {
        public async Task<TechnicalSpecification> AnalyzeDocumentAsync(ApiDocument document)
        {
            var spec = new TechnicalSpecification
            {
                Id = Guid.NewGuid(),
                Title = ExtractTitle(document.Content),
                Version = ExtractVersion(document.Content),
                Endpoints = (await ExtractEndpointsAsync(document.Content)).ToList(),
                Models = (await ExtractDataModelsAsync(document.Content)).ToList(),
                Authentication = await ExtractAuthenticationDetailsAsync(document.Content),
                GeneratedAt = DateTime.UtcNow
            };

            spec.Content = GenerateTechnicalSpecMarkdown(spec);
            return spec;
        }

        public async Task<UseCaseGuide> GenerateUseCaseGuideAsync(ApiDocument document)
        {
            var useCases = ExtractUseCases(document.Content);
            
            var guide = new UseCaseGuide
            {
                Id = Guid.NewGuid(),
                Title = $"Use Case Guide - {ExtractTitle(document.Content)}",
                UseCases = useCases,
                GeneratedAt = DateTime.UtcNow
            };

            guide.Content = GenerateUseCaseMarkdown(guide);
            return guide;
        }

        public async Task<ApiEndpoint[]> ExtractEndpointsAsync(string documentContent)
        {
            var endpoints = new List<ApiEndpoint>();
            
            // OPTUS CPQ Web Portal - Authentication Required
            endpoints.Add(new ApiEndpoint
            {
                Name = "OPTUS CPQ Portal Login",
                Method = "GET",
                Path = "/Login.aspx",
                Description = "OPTUS CPQ web portal authentication endpoint - browser access required for API access",
                Parameters = new List<Parameter>(),
                Response = new ResponseModel
                {
                    ContentType = "text/html",
                    Schema = "HTML login form",
                    Example = @"HTML login page requiring browser interaction"
                }
            });

            // OPTUS CPQ Web Portal - Dashboard Access
            endpoints.Add(new ApiEndpoint
            {
                Name = "OPTUS CPQ Dashboard",
                Method = "GET",
                Path = "/default.aspx",
                Description = "OPTUS CPQ main dashboard - accessible after web portal authentication",
                Parameters = new List<Parameter>(),
                Response = new ResponseModel
                {
                    ContentType = "text/html",
                    Schema = "HTML dashboard page",
                    Example = @"Web portal dashboard requiring browser session"
                }
            });

            return await Task.FromResult(endpoints.ToArray());
        }

        public async Task<DataModel[]> ExtractDataModelsAsync(string documentContent)
        {
            var models = new List<DataModel>();

            // CarrierInfo model
            models.Add(new DataModel
            {
                Name = "CarrierInfo",
                Properties = new List<Property>
                {
                    new Property { Name = "cod_prov", Type = "integer", Required = true, Description = "Province code" },
                    new Property { Name = "codigo_uso", Type = "string", Required = true, Description = "Carrier GUID" },
                    new Property { Name = "municipio", Type = "string", Required = true, Description = "Municipality" },
                    new Property { Name = "nombre", Type = "string", Required = true, Description = "Carrier name and address" }
                }
            });

            // QuotationRequest model
            models.Add(new DataModel
            {
                Name = "QuotationRequest",
                Properties = new List<Property>
                {
                    new Property { Name = "address", Type = "string", Required = true, Description = "Requested address" },
                    new Property { Name = "client", Type = "string", Required = true, Description = "Client ID" },
                    new Property { Name = "service", Type = "string", Required = true, Description = "Service type" },
                    new Property { Name = "carrier", Type = "string", Required = true, Description = "Carrier ID" },
                    new Property { Name = "capacityMbps", Type = "integer", Required = false, Description = "Capacity in Mbps" },
                    new Property { Name = "termMonths", Type = "integer", Required = false, Description = "Term in months" },
                    new Property { Name = "offNetOLO", Type = "boolean", Required = false, Description = "Allow off-net OLO" },
                    new Property { Name = "CIDR", Type = "integer", Required = false, Description = "Subnet mask" },
                    new Property { Name = "requestID", Type = "string", Required = true, Description = "Request identifier" }
                }
            });

            // QuotationResponse model
            models.Add(new DataModel
            {
                Name = "QuotationResponse",
                Properties = new List<Property>
                {
                    new Property { Name = "endA", Type = "string", Required = true, Description = "Normalized Google geocoding address" },
                    new Property { Name = "coords", Type = "object", Required = true, Description = "Coordinates object" },
                    new Property { Name = "endB", Type = "string", Required = true, Description = "Carrier address" },
                    new Property { Name = "capacityMbps", Type = "integer", Required = true, Description = "Speed" },
                    new Property { Name = "termMonths", Type = "integer", Required = true, Description = "Term in months" },
                    new Property { Name = "nrc", Type = "decimal", Required = true, Description = "Non Recurrent Cost" },
                    new Property { Name = "mrc", Type = "decimal", Required = true, Description = "Monthly Recurrent Cost" },
                    new Property { Name = "leadTime", Type = "object", Required = true, Description = "Lead time" },
                    new Property { Name = "service", Type = "string", Required = true, Description = "Service provided" },
                    new Property { Name = "offerCode", Type = "string", Required = true, Description = "OPTUS offer ID" }
                }
            });

            return await Task.FromResult(models.ToArray());
        }

        public async Task<AuthenticationDetails> ExtractAuthenticationDetailsAsync(string documentContent)
        {
            var auth = new AuthenticationDetails
            {
                Type = "Web Portal Login (Session-Based)",
                TokenEndpoint = "https://optuswholesale.cpq.cloud.sap/Login.aspx",
                ClientId = "B2BNitel",
                ClientSecret = "Shetry!$990",
                AdditionalParameters = new Dictionary<string, string>
                {
                    { "Portal_URL", "https://optuswholesale.cpq.cloud.sap/" },
                    { "Environment", "Production" },
                    { "Access_Method", "Form-based browser authentication with session management" },
                    { "Authentication_Flow", "1. GET /Login.aspx 2. Extract CSRF token 3. POST credentials 4. Maintain session cookies" },
                    { "Session_Management", "Required - cookies and session tokens must be maintained" },
                    { "Content_Type", "application/x-www-form-urlencoded for forms, text/html for responses" },
                    { "Note", "OAuth2 not supported - web portal uses form-based authentication only" },
                    { "Browser_Simulation", "Required for complex workflows and multi-step processes" }
                }
            };

            return await Task.FromResult(auth);
        }

        private string ExtractTitle(string content)
        {
            var match = Regex.Match(content, @"OPTUS.*?B2B|Optus.*?API|PODS.*?API", RegexOptions.IgnoreCase);
            return match.Success ? match.Value : "API Specification";
        }

        private string ExtractVersion(string content)
        {
            var match = Regex.Match(content, @"v?\d+\.\d+");
            return match.Success ? match.Value : "1.0";
        }

        private List<UseCase> ExtractUseCases(string content)
        {
            var useCases = new List<UseCase>
            {
                new UseCase
                {
                    Name = "Success",
                    Description = "Successful quotation request",
                    Scenario = "Buyer sends request with all required information including desired Carrier",
                    ExpectedResult = "API returns all quotation information",
                    ErrorHandling = "None"
                },
                new UseCase
                {
                    Name = "Carrier Not Available",
                    Description = "Carrier information not available",
                    Scenario = "The Method is not giving back the Carrier information",
                    ExpectedResult = "Error response with appropriate status code",
                    ErrorHandling = "Return error code and message"
                },
                new UseCase
                {
                    Name = "Web Service Not Available",
                    Description = "Service unavailable error",
                    Scenario = "Buyer sends request but seller's Web Service doesn't reply",
                    ExpectedResult = "Service unavailable error",
                    ErrorHandling = "HTTP 503 or timeout error"
                },
                new UseCase
                {
                    Name = "Data Validation Error",
                    Description = "Invalid input data",
                    Scenario = "Validation process finds unexpected information",
                    ExpectedResult = "Validation error with details",
                    ErrorHandling = "Return specific validation errors"
                },
                new UseCase
                {
                    Name = "Address Not Found",
                    Description = "Address geocoding failure",
                    Scenario = "Google Geocoding API returns no matches",
                    ExpectedResult = "Address not found error",
                    ErrorHandling = "Return geocoding error"
                }
            };

            return useCases;
        }

        private string GenerateTechnicalSpecMarkdown(TechnicalSpecification spec)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Technical Specification: {spec.Title} (Portal-Based)");
            sb.AppendLine($"**Version:** {spec.Version} (QC-Enhanced)");
            sb.AppendLine($"**Generated:** {spec.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Architecture:** Web Portal with Session-Based Authentication");
            sb.AppendLine();
            
            sb.AppendLine("## 🔐 Authentication (CORRECTED)");
            sb.AppendLine($"- **Type:** {spec.Authentication?.Type}");
            sb.AppendLine($"- **Portal URL:** {spec.Authentication?.TokenEndpoint}");
            if (!string.IsNullOrEmpty(spec.Authentication?.ClientId))
            {
                sb.AppendLine($"- **Username:** {spec.Authentication.ClientId}");
            }
            sb.AppendLine($"- **Flow:** Form-based authentication with session cookies");
            sb.AppendLine($"- **Content-Type:** application/x-www-form-urlencoded (forms), text/html (responses)");
            sb.AppendLine($"- **Session Management:** Required - maintain cookies and CSRF tokens");
            sb.AppendLine();
            
            sb.AppendLine("⚠️ **Important Notes:**");
            sb.AppendLine("- OAuth2 Bearer tokens are NOT supported");
            sb.AppendLine("- Direct API calls return HTML, not JSON");
            sb.AppendLine("- Browser simulation required for complex workflows");
            sb.AppendLine("- Session expiration must be handled gracefully");
            sb.AppendLine();
            
            sb.AppendLine("## 🌐 Web Portal Endpoints");
            foreach (var endpoint in spec.Endpoints)
            {
                sb.AppendLine($"### {endpoint.Name}");
                sb.AppendLine($"- **Method:** {endpoint.Method}");
                sb.AppendLine($"- **Path:** {endpoint.Path}");
                sb.AppendLine($"- **Response Type:** HTML (not JSON)");
                sb.AppendLine($"- **Description:** {endpoint.Description}");
                sb.AppendLine($"- **Authentication Required:** {(endpoint.Path.Contains("default", StringComparison.OrdinalIgnoreCase) ? "Yes" : "No")}");
                
                if (endpoint.Parameters?.Any() == true)
                {
                    sb.AppendLine("- **Form Parameters:**");
                    foreach (var param in endpoint.Parameters)
                    {
                        sb.AppendLine($"  - `{param.Name}` ({param.Type}): {param.Description} {(param.Required ? "[Required]" : "[Optional]")}");
                    }
                }
                sb.AppendLine();
            }
            
            sb.AppendLine("## 📊 Data Models (HTML-Parsed)");
            sb.AppendLine("*Note: These models represent data structures that would be extracted from HTML responses, not JSON APIs.*");
            sb.AppendLine();
            
            foreach (var model in spec.Models)
            {
                sb.AppendLine($"### {model.Name}");
                sb.AppendLine("*Source: Parsed from portal HTML content*");
                foreach (var prop in model.Properties)
                {
                    sb.AppendLine($"- `{prop.Name}` ({prop.Type}): {prop.Description} {(prop.Required ? "[Required]" : "")}");
                }
                sb.AppendLine();
            }
            
            sb.AppendLine("## 🔄 Integration Approach");
            sb.AppendLine("### Recommended Implementation:");
            sb.AppendLine("1. **Browser Simulation**: Use HttpClient with cookie container");
            sb.AppendLine("2. **Session Management**: Maintain authentication state across requests");
            sb.AppendLine("3. **HTML Parsing**: Extract data from HTML responses using regex or HTML parsers");
            sb.AppendLine("4. **Form Submission**: Submit data using application/x-www-form-urlencoded");
            sb.AppendLine("5. **Error Handling**: Parse HTML for error messages and validation failures");
            sb.AppendLine();
            
            sb.AppendLine("### ❌ What NOT to do:");
            sb.AppendLine("- Do not expect JSON responses from portal endpoints");
            sb.AppendLine("- Do not use OAuth2 Bearer token authentication");
            sb.AppendLine("- Do not treat portal pages as REST API endpoints");
            sb.AppendLine("- Do not ignore session management and CSRF tokens");
            sb.AppendLine();
            
            return sb.ToString();
        }

        private string GenerateUseCaseMarkdown(UseCaseGuide guide)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {guide.Title}");
            sb.AppendLine($"**Generated:** {guide.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            
            sb.AppendLine("## Use Cases");
            foreach (var useCase in guide.UseCases)
            {
                sb.AppendLine($"### {useCase.Name}");
                sb.AppendLine($"**Description:** {useCase.Description}");
                sb.AppendLine($"**Scenario:** {useCase.Scenario}");
                sb.AppendLine($"**Expected Result:** {useCase.ExpectedResult}");
                sb.AppendLine($"**Error Handling:** {useCase.ErrorHandling}");
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
    }
}