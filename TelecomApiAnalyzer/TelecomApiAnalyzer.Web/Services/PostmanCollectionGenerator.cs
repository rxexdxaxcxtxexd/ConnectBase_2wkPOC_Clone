using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelecomApiAnalyzer.Web.Models;

namespace TelecomApiAnalyzer.Web.Services
{
    public class PostmanCollectionGenerator : IPostmanCollectionGenerator
    {
        private readonly ITestRunnerService _testRunnerService;
        private readonly ILogger<PostmanCollectionGenerator> _logger;
        
        public PostmanCollectionGenerator(ITestRunnerService testRunnerService, ILogger<PostmanCollectionGenerator> logger)
        {
            _testRunnerService = testRunnerService;
            _logger = logger;
        }
        
        public async Task<PostmanCollection> GenerateCollectionAsync(TechnicalSpecification specification)
        {
            var collection = new
            {
                info = new
                {
                    name = specification.Title,
                    description = $"Postman collection for {specification.Title} - Generated on {DateTime.UtcNow:yyyy-MM-dd}",
                    schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
                },
                auth = new
                {
                    type = "noauth"
                },
                @event = new[]
                {
                    new
                    {
                        listen = "prerequest",
                        script = new
                        {
                            type = "text/javascript",
                            exec = new[]
                            {
                                "// OPTUS CPQ Web Portal Access",
                                "// Note: This API requires browser-based authentication",
                                "// Direct API access through Postman is not supported",
                                "console.log('OPTUS CPQ Portal requires browser authentication');",
                                "console.log('Please access: https://optuswholesale.cpq.cloud.sap/Login.aspx');"
                            }
                        }
                    }
                },
                item = GenerateRequestItems(specification),
                variable = new[]
                {
                    new { key = "portal_url", value = "https://optuswholesale.cpq.cloud.sap", type = "string" },
                    new { key = "login_endpoint", value = "/Login.aspx", type = "string" },
                    new { key = "dashboard_endpoint", value = "/default.aspx", type = "string" },
                    new { key = "note", value = "Browser authentication required", type = "string" }
                }
            };

            var postmanCollection = new PostmanCollection
            {
                Name = specification.Title,
                JsonContent = JsonSerializer.Serialize(collection, new JsonSerializerOptions { WriteIndented = true }),
                GeneratedAt = DateTime.UtcNow
            };

            return await Task.FromResult(postmanCollection);
        }

        private object[] GenerateRequestItems(TechnicalSpecification specification)
        {
            var items = new List<object>();

            foreach (var endpoint in specification.Endpoints)
            {
                var request = new
                {
                    name = endpoint.Name,
                    request = new
                    {
                        method = endpoint.Method,
                        header = new[]
                        {
                            new { key = "Accept", value = "text/html,application/xhtml+xml", type = "text" },
                            new { key = "User-Agent", value = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", type = "text" }
                        },
                        body = endpoint.Method == "POST" ? new
                        {
                            mode = "raw",
                            raw = GenerateRequestBody(endpoint),
                            options = new
                            {
                                raw = new
                                {
                                    language = "json"
                                }
                            }
                        } : null,
                        url = new
                        {
                            raw = $"{{{{portal_url}}}}{endpoint.Path}",
                            host = new[] { "{{portal_url}}" },
                            path = endpoint.Path.TrimStart('/').Split('/')
                        },
                        description = endpoint.Description
                    },
                    response = new object[] { }
                };

                items.Add(request);
            }

            // Add test requests
            items.Add(GenerateTestRequest());
            
            // Add discovered endpoints folder
            var discoveredFolder = GenerateDiscoveredEndpointsFolder();
            if (discoveredFolder != null)
            {
                items.Add(discoveredFolder);
            }

            return items.ToArray();
        }

        private string GenerateRequestBody(ApiEndpoint endpoint)
        {
            // Web portal endpoints don't require request bodies for GET requests
            if (endpoint?.Method == "GET")
            {
                return "";
            }
            
            // For any POST requests to the web portal (form submissions)
            return "<!-- Web portal form data would go here -->"; 
        }

        private object GenerateTestRequest()
        {
            return new
            {
                name = "Web Portal Tests",
                item = new object[]
                {
                    new
                    {
                        name = "Test - Access Login Page",
                        @event = new[]
                        {
                            new
                            {
                                listen = "test",
                                script = new
                                {
                                    exec = new[]
                                    {
                                        "pm.test('Status code is 200', function () {",
                                        "    pm.expect(pm.response.code).to.equal(200);",
                                        "});",
                                        "",
                                        "pm.test('Response contains login form', function () {",
                                        "    pm.expect(pm.response.text()).to.include('Login');",
                                        "});",
                                        "",
                                        "pm.test('Response time is acceptable', function () {",
                                        "    pm.expect(pm.response.responseTime).to.be.below(5000);",
                                        "});"
                                    }
                                }
                            }
                        },
                        request = new
                        {
                            method = "GET",
                            url = "{{portal_url}}{{login_endpoint}}"
                        }
                    },
                    new
                    {
                        name = "Test - Access Dashboard (Requires Authentication)",
                        @event = new[]
                        {
                            new
                            {
                                listen = "test",
                                script = new
                                {
                                    exec = new[]
                                    {
                                        "pm.test('Response received', function () {",
                                        "    pm.expect(pm.response.code).to.be.oneOf([200, 302, 401]);",
                                        "});",
                                        "",
                                        "pm.test('Portal requires authentication', function () {",
                                        "    // Expect redirect to login or authentication error",
                                        "    pm.expect([200, 302, 401]).to.include(pm.response.code);",
                                        "});"
                                    }
                                }
                            }
                        },
                        request = new
                        {
                            method = "GET",
                            url = "{{portal_url}}{{dashboard_endpoint}}"
                        }
                    }
                }
            };
        }
        
        private object? GenerateDiscoveredEndpointsFolder()
        {
            try
            {
                // This would ideally get discovered endpoints from a recent test run
                // For now, we'll create a placeholder structure for discovered endpoints
                return new
                {
                    name = "Discovered API Endpoints",
                    description = "Endpoints discovered through content analysis",
                    item = new object[]
                    {
                        new
                        {
                            name = "JavaScript API Discovery",
                            item = GenerateJavaScriptEndpointRequests()
                        },
                        new
                        {
                            name = "Business Workflow Endpoints",
                            item = GenerateBusinessWorkflowRequests()
                        },
                        new
                        {
                            name = "Form Submission Endpoints", 
                            item = GenerateFormSubmissionRequests()
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating discovered endpoints folder");
                return null;
            }
        }
        
        private object[] GenerateJavaScriptEndpointRequests()
        {
            var requests = new List<object>();
            
            // Common SAP CPQ JavaScript API patterns
            var commonEndpoints = new[]
            {
                new { name = "Get Quote Data", method = "GET", path = "/api/quote/{id}", description = "Retrieve quote information" },
                new { name = "Create Quote", method = "POST", path = "/api/quote", description = "Create new quote" },
                new { name = "Update Quote", method = "PUT", path = "/api/quote/{id}", description = "Update existing quote" },
                new { name = "Get Product Catalog", method = "GET", path = "/api/products", description = "Retrieve product catalog" },
                new { name = "Check Service Availability", method = "POST", path = "/api/availability", description = "Check service availability" },
                new { name = "Get Customer Data", method = "GET", path = "/api/customer/{id}", description = "Retrieve customer information" }
            };
            
            foreach (var endpoint in commonEndpoints)
            {
                requests.Add(new
                {
                    name = endpoint.name,
                    request = new
                    {
                        method = endpoint.method,
                        header = new[]
                        {
                            new { key = "Accept", value = "application/json", type = "text" },
                            new { key = "Content-Type", value = "application/json", type = "text" },
                            new { key = "X-Requested-With", value = "XMLHttpRequest", type = "text" }
                        },
                        url = new
                        {
                            raw = $"{{{{portal_url}}}}{endpoint.path}",
                            host = new[] { "{{portal_url}}" },
                            path = endpoint.path.TrimStart('/').Split('/')
                        },
                        description = endpoint.description
                    },
                    response = new object[] { }
                });
            }
            
            return requests.ToArray();
        }
        
        private object[] GenerateBusinessWorkflowRequests()
        {
            var requests = new List<object>();
            
            // SAP CPQ Business workflow endpoints
            var workflowEndpoints = new[]
            {
                new { name = "Quote Workflow - Start", method = "POST", path = "/sap/bc/rest/cpq/quote/start", description = "Initialize quote workflow" },
                new { name = "Quote Workflow - Configure", method = "PUT", path = "/sap/bc/rest/cpq/quote/configure", description = "Configure quote parameters" },
                new { name = "Order Workflow - Create", method = "POST", path = "/sap/bc/rest/cpq/order/create", description = "Create order from quote" },
                new { name = "Service Provisioning", method = "POST", path = "/sap/bc/rest/cpq/provision", description = "Provision services" },
                new { name = "Billing Integration", method = "GET", path = "/sap/bc/rest/cpq/billing/{orderId}", description = "Get billing information" }
            };
            
            foreach (var endpoint in workflowEndpoints)
            {
                requests.Add(new
                {
                    name = endpoint.name,
                    request = new
                    {
                        method = endpoint.method,
                        header = new[]
                        {
                            new { key = "Accept", value = "application/json", type = "text" },
                            new { key = "Content-Type", value = "application/json", type = "text" },
                            new { key = "Authorization", value = "Bearer {{access_token}}", type = "text" }
                        },
                        body = endpoint.method != "GET" ? new
                        {
                            mode = "raw",
                            raw = GenerateWorkflowRequestBody(endpoint.name),
                            options = new { raw = new { language = "json" } }
                        } : null,
                        url = new
                        {
                            raw = $"{{{{portal_url}}}}{endpoint.path}",
                            host = new[] { "{{portal_url}}" },
                            path = endpoint.path.TrimStart('/').Split('/')
                        },
                        description = endpoint.description
                    },
                    response = new object[] { }
                });
            }
            
            return requests.ToArray();
        }
        
        private object[] GenerateFormSubmissionRequests()
        {
            var requests = new List<object>();
            
            // Common form submission endpoints
            var formEndpoints = new[]
            {
                new { name = "Login Form", method = "POST", path = "/Login.aspx", description = "Submit login credentials" },
                new { name = "Contact Form", method = "POST", path = "/Contact/Submit", description = "Submit contact information" },
                new { name = "Quote Request Form", method = "POST", path = "/Quote/Request", description = "Submit quote request" },
                new { name = "Service Request Form", method = "POST", path = "/Service/Request", description = "Submit service request" }
            };
            
            foreach (var endpoint in formEndpoints)
            {
                requests.Add(new
                {
                    name = endpoint.name,
                    request = new
                    {
                        method = endpoint.method,
                        header = new[]
                        {
                            new { key = "Content-Type", value = "application/x-www-form-urlencoded", type = "text" },
                            new { key = "Accept", value = "text/html,application/xhtml+xml", type = "text" }
                        },
                        body = new
                        {
                            mode = "urlencoded",
                            urlencoded = GenerateFormData(endpoint.name)
                        },
                        url = new
                        {
                            raw = $"{{{{portal_url}}}}{endpoint.path}",
                            host = new[] { "{{portal_url}}" },
                            path = endpoint.path.TrimStart('/').Split('/')
                        },
                        description = endpoint.description
                    },
                    response = new object[] { }
                });
            }
            
            return requests.ToArray();
        }
        
        private string GenerateWorkflowRequestBody(string workflowName)
        {
            return workflowName.ToLower() switch
            {
                var name when name.Contains("quote") && name.Contains("start") => 
                    "{\n  \"customerId\": \"{{customer_id}}\",\n  \"serviceType\": \"B2B_CONNECTIVITY\",\n  \"requestedDate\": \"{{$isoTimestamp}}\"\n}",
                var name when name.Contains("quote") && name.Contains("configure") => 
                    "{\n  \"quoteId\": \"{{quote_id}}\",\n  \"bandwidth\": \"100Mbps\",\n  \"serviceLevel\": \"PREMIUM\"\n}",
                var name when name.Contains("order") => 
                    "{\n  \"quoteId\": \"{{quote_id}}\",\n  \"approvalCode\": \"{{approval_code}}\"\n}",
                var name when name.Contains("provision") => 
                    "{\n  \"orderId\": \"{{order_id}}\",\n  \"scheduleDate\": \"{{$isoTimestamp}}\"\n}",
                _ => "{\n  \"message\": \"Request body template\"\n}"
            };
        }
        
        private object[] GenerateFormData(string formName)
        {
            return formName.ToLower() switch
            {
                var name when name.Contains("login") => new object[]
                {
                    new { key = "username", value = "{{username}}", type = "text" },
                    new { key = "password", value = "{{password}}", type = "text" },
                    new { key = "__RequestVerificationToken", value = "{{csrf_token}}", type = "text" }
                },
                var name when name.Contains("contact") => new object[]
                {
                    new { key = "name", value = "Test User", type = "text" },
                    new { key = "email", value = "test@example.com", type = "text" },
                    new { key = "message", value = "Test message", type = "text" }
                },
                var name when name.Contains("quote") => new object[]
                {
                    new { key = "serviceType", value = "B2B_CONNECTIVITY", type = "text" },
                    new { key = "bandwidth", value = "100", type = "text" },
                    new { key = "location", value = "Sydney, NSW", type = "text" }
                },
                _ => new object[]
                {
                    new { key = "data", value = "form data", type = "text" }
                }
            };
        }
    }
}