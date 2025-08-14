using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelecomApiAnalyzer.Web.Models;

namespace TelecomApiAnalyzer.Web.Services
{
    public class CodeGenerationService : ICodeGenerationService
    {
        public async Task<GeneratedCode> GenerateCodeAsync(TechnicalSpecification specification)
        {
            var generatedCode = new GeneratedCode
            {
                Id = Guid.NewGuid(),
                Language = "C#",
                GeneratedAt = DateTime.UtcNow,
                Files = new Dictionary<string, string>()
            };

            generatedCode.Files["Models.cs"] = await GenerateModelsAsync(specification);
            generatedCode.Files["IApiService.cs"] = await GenerateServiceInterfaceAsync(specification);
            generatedCode.Files["ApiService.cs"] = await GenerateServiceImplementationAsync(specification);
            generatedCode.Files["ApiClient.cs"] = await GenerateApiClientAsync(specification);

            return generatedCode;
        }

        public async Task<string> GenerateApiClientAsync(TechnicalSpecification specification)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using System.Net.Http.Headers;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using System.Text.Json;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Microsoft.Extensions.Configuration;");
            sb.AppendLine("using Microsoft.Extensions.Logging;");
            sb.AppendLine();
            sb.AppendLine("namespace GeneratedApi.Client");
            sb.AppendLine("{");
            sb.AppendLine("    public class ApiClient");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly HttpClient _httpClient;");
            sb.AppendLine("        private readonly ILogger<ApiClient> _logger;");
            sb.AppendLine("        private readonly IConfiguration _configuration;");
            sb.AppendLine("        private string _accessToken;");
            sb.AppendLine();
            sb.AppendLine("        public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger, IConfiguration configuration)");
            sb.AppendLine("        {");
            sb.AppendLine("            _httpClient = httpClient;");
            sb.AppendLine("            _logger = logger;");
            sb.AppendLine("            _configuration = configuration;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private async Task<string> GetAccessTokenAsync()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (!string.IsNullOrEmpty(_accessToken))");
            sb.AppendLine("                return _accessToken;");
            sb.AppendLine();
            sb.AppendLine("            var tokenEndpoint = _configuration[\"ApiSettings:TokenEndpoint\"];");
            sb.AppendLine("            var clientId = _configuration[\"ApiSettings:ClientId\"];");
            sb.AppendLine("            var clientSecret = _configuration[\"ApiSettings:ClientSecret\"];");
            sb.AppendLine();
            sb.AppendLine("            var tokenRequest = new FormUrlEncodedContent(new[]");
            sb.AppendLine("            {");
            sb.AppendLine("                new KeyValuePair<string, string>(\"grant_type\", \"client_credentials\"),");
            sb.AppendLine("                new KeyValuePair<string, string>(\"client_id\", clientId),");
            sb.AppendLine("                new KeyValuePair<string, string>(\"client_secret\", clientSecret)");
            sb.AppendLine("            });");
            sb.AppendLine();
            sb.AppendLine("            var response = await _httpClient.PostAsync(tokenEndpoint, tokenRequest);");
            sb.AppendLine("            response.EnsureSuccessStatusCode();");
            sb.AppendLine();
            sb.AppendLine("            var content = await response.Content.ReadAsStringAsync();");
            sb.AppendLine("            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);");
            sb.AppendLine("            _accessToken = tokenResponse.AccessToken;");
            sb.AppendLine();
            sb.AppendLine("            return _accessToken;");
            sb.AppendLine("        }");
            sb.AppendLine();

            foreach (var endpoint in specification.Endpoints)
            {
                var methodName = endpoint.Name.Replace(" ", "");
                var returnType = endpoint.Method == "GET" ? "List<CarrierInfo>" : "QuotationResponse";
                
                sb.AppendLine($"        public async Task<{returnType}> {methodName}Async({GenerateMethodParameters(endpoint)})");
                sb.AppendLine("        {");
                sb.AppendLine("            var token = await GetAccessTokenAsync();");
                sb.AppendLine("            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(\"Bearer\", token);");
                sb.AppendLine();
                
                if (endpoint.Method == "GET")
                {
                    sb.AppendLine($"            var response = await _httpClient.GetAsync(\"{endpoint.Path}\");");
                }
                else
                {
                    sb.AppendLine("            var json = JsonSerializer.Serialize(request);");
                    sb.AppendLine("            var content = new StringContent(json, Encoding.UTF8, \"application/json\");");
                    sb.AppendLine($"            var response = await _httpClient.PostAsync(\"{endpoint.Path}\", content);");
                }
                
                sb.AppendLine("            response.EnsureSuccessStatusCode();");
                sb.AppendLine();
                sb.AppendLine("            var responseContent = await response.Content.ReadAsStringAsync();");
                sb.AppendLine($"            return JsonSerializer.Deserialize<{returnType}>(responseContent);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public class TokenResponse");
            sb.AppendLine("    {");
            sb.AppendLine("        public string AccessToken { get; set; }");
            sb.AppendLine("        public string TokenType { get; set; }");
            sb.AppendLine("        public int ExpiresIn { get; set; }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        public async Task<string> GenerateModelsAsync(TechnicalSpecification specification)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine();
            sb.AppendLine("namespace GeneratedApi.Models");
            sb.AppendLine("{");

            foreach (var model in specification.Models)
            {
                sb.AppendLine($"    public class {model.Name}");
                sb.AppendLine("    {");
                
                foreach (var prop in model.Properties)
                {
                    var csharpType = MapToCSharpType(prop.Type);
                    var jsonPropertyName = ToCamelCase(prop.Name);
                    
                    sb.AppendLine($"        [JsonPropertyName(\"{jsonPropertyName}\")]");
                    sb.AppendLine($"        public {csharpType} {ToPascalCase(prop.Name)} {{ get; set; }}");
                    sb.AppendLine();
                }
                
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        public async Task<string> GenerateServiceInterfaceAsync(TechnicalSpecification specification)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using GeneratedApi.Models;");
            sb.AppendLine();
            sb.AppendLine("namespace GeneratedApi.Services");
            sb.AppendLine("{");
            sb.AppendLine("    public interface IApiService");
            sb.AppendLine("    {");

            foreach (var endpoint in specification.Endpoints)
            {
                var methodName = endpoint.Name.Replace(" ", "");
                var returnType = endpoint.Method == "GET" ? "List<CarrierInfo>" : "QuotationResponse";
                
                sb.AppendLine($"        Task<{returnType}> {methodName}Async({GenerateMethodParameters(endpoint)});");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        public async Task<string> GenerateServiceImplementationAsync(TechnicalSpecification specification)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using GeneratedApi.Models;");
            sb.AppendLine("using GeneratedApi.Client;");
            sb.AppendLine("using Microsoft.Extensions.Logging;");
            sb.AppendLine();
            sb.AppendLine("namespace GeneratedApi.Services");
            sb.AppendLine("{");
            sb.AppendLine("    public class ApiService : IApiService");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly ApiClient _apiClient;");
            sb.AppendLine("        private readonly ILogger<ApiService> _logger;");
            sb.AppendLine();
            sb.AppendLine("        public ApiService(ApiClient apiClient, ILogger<ApiService> logger)");
            sb.AppendLine("        {");
            sb.AppendLine("            _apiClient = apiClient;");
            sb.AppendLine("            _logger = logger;");
            sb.AppendLine("        }");
            sb.AppendLine();

            foreach (var endpoint in specification.Endpoints)
            {
                var methodName = endpoint.Name.Replace(" ", "");
                var returnType = endpoint.Method == "GET" ? "List<CarrierInfo>" : "QuotationResponse";
                
                sb.AppendLine($"        public async Task<{returnType}> {methodName}Async({GenerateMethodParameters(endpoint)})");
                sb.AppendLine("        {");
                sb.AppendLine("            try");
                sb.AppendLine("            {");
                sb.AppendLine($"                _logger.LogInformation(\"Calling {endpoint.Name}\");");
                
                if (endpoint.Method == "GET")
                {
                    sb.AppendLine($"                return await _apiClient.{methodName}Async();");
                }
                else
                {
                    sb.AppendLine($"                return await _apiClient.{methodName}Async(request);");
                }
                
                sb.AppendLine("            }");
                sb.AppendLine("            catch (Exception ex)");
                sb.AppendLine("            {");
                sb.AppendLine($"                _logger.LogError(ex, \"Error calling {endpoint.Name}\");");
                sb.AppendLine("                throw;");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        private string GenerateMethodParameters(ApiEndpoint endpoint)
        {
            if (endpoint.Method == "GET")
                return "";

            return "QuotationRequest request";
        }

        private string MapToCSharpType(string type)
        {
            return type?.ToLower() switch
            {
                "string" => "string",
                "integer" => "int",
                "decimal" => "decimal",
                "boolean" => "bool",
                "object" => "object",
                _ => "string"
            };
        }

        private string ToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            return char.ToLower(input[0]) + input.Substring(1);
        }

        private string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}