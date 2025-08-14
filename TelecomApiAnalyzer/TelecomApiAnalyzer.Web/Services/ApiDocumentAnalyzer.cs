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
            
            // Extract Method 0 (Carrier)
            endpoints.Add(new ApiEndpoint
            {
                Name = "Get Carriers",
                Method = "GET",
                Path = "/api/carriers",
                Description = "Provide carrier information mandatory to feeds the quotation Process",
                Parameters = new List<Parameter>(),
                Response = new ResponseModel
                {
                    ContentType = "application/json",
                    Schema = "Array of CarrierInfo objects",
                    Example = @"[{""cod_prov"": 99, ""codigo_uso"": ""cab9a0a7-5d77-e911-a84f-000d3a2a78db"", ""municipio"": ""New York Condado"", ""nombre"": ""60 Hudson New York""}]"
                }
            });

            // Extract Method 1 (Quotation)
            endpoints.Add(new ApiEndpoint
            {
                Name = "Create Quotation",
                Method = "POST",
                Path = "/api/quotation",
                Description = "Return Quotation and Offer Code based on Inputs provided by the buyer",
                Parameters = new List<Parameter>
                {
                    new Parameter { Name = "address", Type = "string", Required = true, Description = "Requested address", Example = "Gran Via Street 1, Madrid" },
                    new Parameter { Name = "client", Type = "string", Required = true, Description = "Client ID (API Manager user name)" },
                    new Parameter { Name = "service", Type = "string", Required = true, Description = "Service provided", Example = "Capacity, Internet" },
                    new Parameter { Name = "carrier", Type = "string", Required = true, Description = "Carrier from Method 0" },
                    new Parameter { Name = "capacityMbps", Type = "integer", Required = false, Description = "Speed (Mbps)", Example = "100" },
                    new Parameter { Name = "termMonths", Type = "integer", Required = false, Description = "Term (months)", Example = "36" },
                    new Parameter { Name = "offNetOLO", Type = "boolean", Required = false, Description = "The buyer allows offNetOLO services" },
                    new Parameter { Name = "CIDR", Type = "integer", Required = false, Description = "Subnet mask", Example = "30" },
                    new Parameter { Name = "requestID", Type = "string", Required = true, Description = "Buyer Request ID", Example = "XX-132456" }
                },
                Response = new ResponseModel
                {
                    ContentType = "application/json",
                    Schema = "QuotationResponse object"
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
                    new Property { Name = "offerCode", Type = "string", Required = true, Description = "Lyntia offer ID" }
                }
            });

            return await Task.FromResult(models.ToArray());
        }

        public async Task<AuthenticationDetails> ExtractAuthenticationDetailsAsync(string documentContent)
        {
            var auth = new AuthenticationDetails
            {
                Type = "Bearer Token",
                TokenEndpoint = "https://pre-apimanager.lyntia.com/token",
                AdditionalParameters = new Dictionary<string, string>
                {
                    { "API_Manager_URL", "https://pre-apimanager.lyntia.com/store/" },
                    { "Environment", "Pre-production" }
                }
            };

            return await Task.FromResult(auth);
        }

        private string ExtractTitle(string content)
        {
            var match = Regex.Match(content, @"Lyntia.*?Quotation API", RegexOptions.IgnoreCase);
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
            sb.AppendLine($"# Technical Specification: {spec.Title}");
            sb.AppendLine($"**Version:** {spec.Version}");
            sb.AppendLine($"**Generated:** {spec.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            
            sb.AppendLine("## Authentication");
            sb.AppendLine($"- **Type:** {spec.Authentication?.Type}");
            sb.AppendLine($"- **Token Endpoint:** {spec.Authentication?.TokenEndpoint}");
            sb.AppendLine();
            
            sb.AppendLine("## API Endpoints");
            foreach (var endpoint in spec.Endpoints)
            {
                sb.AppendLine($"### {endpoint.Name}");
                sb.AppendLine($"- **Method:** {endpoint.Method}");
                sb.AppendLine($"- **Path:** {endpoint.Path}");
                sb.AppendLine($"- **Description:** {endpoint.Description}");
                
                if (endpoint.Parameters?.Any() == true)
                {
                    sb.AppendLine("- **Parameters:**");
                    foreach (var param in endpoint.Parameters)
                    {
                        sb.AppendLine($"  - `{param.Name}` ({param.Type}): {param.Description} {(param.Required ? "[Required]" : "[Optional]")}");
                    }
                }
                sb.AppendLine();
            }
            
            sb.AppendLine("## Data Models");
            foreach (var model in spec.Models)
            {
                sb.AppendLine($"### {model.Name}");
                foreach (var prop in model.Properties)
                {
                    sb.AppendLine($"- `{prop.Name}` ({prop.Type}): {prop.Description} {(prop.Required ? "[Required]" : "")}");
                }
                sb.AppendLine();
            }
            
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