using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TelecomApiAnalyzer.Web.Models;

namespace TelecomApiAnalyzer.Web.Services
{
    public class PostmanCollectionGenerator : IPostmanCollectionGenerator
    {
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
                    type = "bearer",
                    bearer = new[]
                    {
                        new
                        {
                            key = "token",
                            value = "{{access_token}}",
                            type = "string"
                        }
                    }
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
                                "// Get access token if not present or expired",
                                "const tokenUrl = pm.environment.get('token_endpoint');",
                                "const clientId = pm.environment.get('client_id');",
                                "const clientSecret = pm.environment.get('client_secret');",
                                "",
                                "if (!pm.environment.get('access_token')) {",
                                "    pm.sendRequest({",
                                "        url: tokenUrl,",
                                "        method: 'POST',",
                                "        header: {",
                                "            'Content-Type': 'application/x-www-form-urlencoded'",
                                "        },",
                                "        body: {",
                                "            mode: 'urlencoded',",
                                "            urlencoded: [",
                                "                {key: 'grant_type', value: 'client_credentials'},",
                                "                {key: 'client_id', value: clientId},",
                                "                {key: 'client_secret', value: clientSecret}",
                                "            ]",
                                "        }",
                                "    }, function (err, res) {",
                                "        if (!err) {",
                                "            const token = res.json().access_token;",
                                "            pm.environment.set('access_token', token);",
                                "        }",
                                "    });",
                                "}"
                            }
                        }
                    }
                },
                item = GenerateRequestItems(specification),
                variable = new[]
                {
                    new { key = "base_url", value = "https://pre-apimanager.lyntia.com", type = "string" },
                    new { key = "token_endpoint", value = specification.Authentication?.TokenEndpoint ?? "", type = "string" },
                    new { key = "client_id", value = "", type = "string" },
                    new { key = "client_secret", value = "", type = "string" }
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
                            new { key = "Content-Type", value = "application/json", type = "text" },
                            new { key = "Accept", value = "application/json", type = "text" }
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
                            raw = $"{{{{base_url}}}}{endpoint.Path}",
                            host = new[] { "{{base_url}}" },
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

            return items.ToArray();
        }

        private string GenerateRequestBody(ApiEndpoint endpoint)
        {
            if (endpoint.Name.Contains("Quotation"))
            {
                return @"{
    ""address"": ""Gran Vía 39, Madrid"",
    ""client"": ""lyntia"",
    ""service"": ""capacidad"",
    ""carrier"": ""ecc6a6a8-5d77-e911-a84c-000d3a2a711c"",
    ""capacityMbps"": 100,
    ""termMonths"": 36,
    ""offNetOLO"": true,
    ""CIDR"": 32,
    ""requestID"": ""TEST-{{$timestamp}}""
}";
            }
            return "{}";
        }

        private object GenerateTestRequest()
        {
            return new
            {
                name = "Test Suite",
                item = new[]
                {
                    new
                    {
                        name = "Test - Get Carriers",
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
                                        "    pm.response.to.have.status(200);",
                                        "});",
                                        "",
                                        "pm.test('Response is an array', function () {",
                                        "    const jsonData = pm.response.json();",
                                        "    pm.expect(jsonData).to.be.an('array');",
                                        "});",
                                        "",
                                        "pm.test('Carriers have required fields', function () {",
                                        "    const jsonData = pm.response.json();",
                                        "    if (jsonData.length > 0) {",
                                        "        pm.expect(jsonData[0]).to.have.property('codigo_uso');",
                                        "        pm.expect(jsonData[0]).to.have.property('nombre');",
                                        "    }",
                                        "});"
                                    }
                                }
                            }
                        },
                        request = new
                        {
                            method = "GET",
                            url = "{{base_url}}/api/carriers"
                        }
                    },
                    new
                    {
                        name = "Test - Create Quotation",
                        @event = new[]
                        {
                            new
                            {
                                listen = "test",
                                script = new
                                {
                                    exec = new[]
                                    {
                                        "pm.test('Status code is 200 or 206', function () {",
                                        "    pm.expect(pm.response.code).to.be.oneOf([200, 206]);",
                                        "});",
                                        "",
                                        "pm.test('Response has offer code', function () {",
                                        "    const jsonData = pm.response.json();",
                                        "    pm.expect(jsonData).to.have.property('offerCode');",
                                        "});",
                                        "",
                                        "pm.test('Response time is less than 5000ms', function () {",
                                        "    pm.expect(pm.response.responseTime).to.be.below(5000);",
                                        "});"
                                    }
                                }
                            }
                        },
                        request = new
                        {
                            method = "POST",
                            body = new
                            {
                                mode = "raw",
                                raw = GenerateRequestBody(new ApiEndpoint { Name = "Quotation" })
                            },
                            url = "{{base_url}}/api/quotation"
                        }
                    }
                }
            };
        }
    }
}