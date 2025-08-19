using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelecomApiAnalyzer.Web.Models;

namespace TelecomApiAnalyzer.Web.Services
{
    public class TestRunnerService : ITestRunnerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TestRunnerService> _logger;
        private static readonly ConcurrentDictionary<Guid, TestSuite> _testSuites = new();

        public TestRunnerService(HttpClient httpClient, ILogger<TestRunnerService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<TestSuite> RunTestSuiteAsync(ApiAnalysisProject project, TestConfiguration configuration, CancellationToken cancellationToken = default)
        {
            var testSuite = new TestSuite
            {
                Id = Guid.NewGuid(),
                Name = $"Test Suite - {project.Name}",
                BaseUrl = configuration.BaseUrl,
                StartedAt = DateTime.UtcNow,
                Status = TestStatus.Running
            };

            _testSuites[testSuite.Id] = testSuite;
            _logger.LogInformation("Starting test suite {TestSuiteId} for project {ProjectName}", testSuite.Id, project.Name);

            try
            {
                // Generate test cases
                testSuite.TestCases = await GenerateTestCasesAsync(project);
                
                // Run each test case
                foreach (var testCase in testSuite.TestCases)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        testSuite.Status = TestStatus.Cancelled;
                        break;
                    }

                    await RunTestCaseAsync(testCase, configuration, cancellationToken);
                }

                testSuite.CompletedAt = DateTime.UtcNow;
                testSuite.Status = testSuite.FailedCount > 0 ? TestStatus.Failed : TestStatus.Passed;
                
                _logger.LogInformation("Completed test suite {TestSuiteId}. Passed: {Passed}, Failed: {Failed}", 
                    testSuite.Id, testSuite.PassedCount, testSuite.FailedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running test suite {TestSuiteId}", testSuite.Id);
                testSuite.Status = TestStatus.Failed;
                testSuite.CompletedAt = DateTime.UtcNow;
            }

            return testSuite;
        }

        public async Task<TestCase> RunTestCaseAsync(TestCase testCase, TestConfiguration configuration, CancellationToken cancellationToken = default)
        {
            testCase.StartedAt = DateTime.UtcNow;
            testCase.Status = TestStatus.Running;

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Create a new configured HTTP client for this test case
                using var httpClient = CreateConfiguredHttpClient(configuration);

                // Build request
                var request = BuildHttpRequest(testCase, configuration);

                // Execute request
                using var response = await httpClient.SendAsync(request, cancellationToken);
                
                stopwatch.Stop();
                testCase.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                testCase.ResponseStatusCode = (int)response.StatusCode;
                testCase.ResponseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                // Run assertions
                await RunAssertionsAsync(testCase, response, configuration);

                testCase.Status = testCase.Assertions.All(a => a.Passed) ? TestStatus.Passed : TestStatus.Failed;
                
                _logger.LogInformation("Test case {TestCaseName} completed in {Duration}ms with status {Status}", 
                    testCase.Name, testCase.ResponseTimeMs, testCase.Status);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                testCase.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                testCase.Status = TestStatus.Failed;
                testCase.ErrorMessage = ex.Message;
                
                _logger.LogError(ex, "Test case {TestCaseName} failed", testCase.Name);
            }

            testCase.CompletedAt = DateTime.UtcNow;
            return testCase;
        }

        public async Task<List<TestCase>> GenerateTestCasesAsync(ApiAnalysisProject project)
        {
            var testCases = new List<TestCase>();

            if (project.Document?.TechnicalSpec?.Endpoints == null)
                return testCases;

            foreach (var endpoint in project.Document.TechnicalSpec.Endpoints)
            {
                // Generate positive test case
                var positiveTest = new TestCase
                {
                    Id = Guid.NewGuid(),
                    Name = $"{endpoint.Method} {endpoint.Path} - Valid Request",
                    Description = $"Test successful {endpoint.Method} request to {endpoint.Path}",
                    Method = endpoint.Method,
                    Endpoint = endpoint.Path,
                    Status = TestStatus.Pending
                };

                // Add request body for POST requests
                if (endpoint.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    positiveTest.RequestBody = GenerateRequestBody(endpoint);
                }

                // Generate negative test cases
                var negativeTests = GenerateNegativeTestCases(endpoint);

                testCases.Add(positiveTest);
                testCases.AddRange(negativeTests);
            }

            return await Task.FromResult(testCases);
        }

        public async Task<TestSuite?> GetTestSuiteAsync(Guid testSuiteId)
        {
            _testSuites.TryGetValue(testSuiteId, out var testSuite);
            return await Task.FromResult(testSuite);
        }

        public async Task<List<TestSuite>> GetTestSuitesAsync(Guid projectId)
        {
            // In a real implementation, this would query a database
            var testSuites = _testSuites.Values.ToList();
            return await Task.FromResult(testSuites);
        }

        public async Task<bool> ValidateEndpointAsync(string baseUrl, string endpoint, TestConfiguration configuration)
        {
            try
            {
                using var httpClient = CreateConfiguredHttpClient(configuration);
                var request = new HttpRequestMessage(HttpMethod.Options, $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}");
                
                using var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private HttpClient CreateConfiguredHttpClient(TestConfiguration configuration)
        {
            var httpClient = new HttpClient();
            
            // Set timeout
            httpClient.Timeout = TimeSpan.FromMilliseconds(configuration.TimeoutMs);

            // Add authentication
            if (configuration.Authentication != null)
            {
                switch (configuration.Authentication.Type?.ToLower())
                {
                    case "bearer":
                    case "bearer token":
                        if (!string.IsNullOrEmpty(configuration.Authentication.ClientSecret))
                        {
                            httpClient.DefaultRequestHeaders.Authorization = 
                                new AuthenticationHeaderValue("Bearer", configuration.Authentication.ClientSecret);
                        }
                        break;
                    case "apikey":
                    case "api key":
                        if (!string.IsNullOrEmpty(configuration.Authentication.ClientId) && 
                            !string.IsNullOrEmpty(configuration.Authentication.ClientSecret))
                        {
                            httpClient.DefaultRequestHeaders.Add(configuration.Authentication.ClientId, configuration.Authentication.ClientSecret);
                        }
                        break;
                }
            }

            // Add global headers (excluding Content-Type which should be set on HttpContent)
            foreach (var header in configuration.GlobalHeaders)
            {
                if (!header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                    catch (InvalidOperationException)
                    {
                        // Skip headers that can't be added to request headers
                    }
                }
            }

            // Set accept header for JSON responses
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            return httpClient;
        }

        private HttpRequestMessage BuildHttpRequest(TestCase testCase, TestConfiguration configuration)
        {
            var url = $"{configuration.BaseUrl.TrimEnd('/')}/{testCase.Endpoint.TrimStart('/')}";
            var method = new HttpMethod(testCase.Method.ToUpper());
            var request = new HttpRequestMessage(method, url);

            // Add headers
            foreach (var header in testCase.Headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            // Add request body for POST/PUT requests
            if (!string.IsNullOrEmpty(testCase.RequestBody) && 
                (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
            {
                request.Content = new StringContent(testCase.RequestBody, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private async Task RunAssertionsAsync(TestCase testCase, HttpResponseMessage response, TestConfiguration configuration)
        {
            testCase.Assertions.Clear();

            // Status code assertion
            var statusAssertion = new TestAssertion
            {
                Name = "Response Status",
                Description = "HTTP response should be successful"
            };

            if (response.IsSuccessStatusCode)
            {
                statusAssertion.Passed = true;
                statusAssertion.ActualValue = $"{(int)response.StatusCode} {response.StatusCode}";
                statusAssertion.ExpectedValue = "2xx Success";
            }
            else
            {
                statusAssertion.Passed = false;
                statusAssertion.ActualValue = $"{(int)response.StatusCode} {response.StatusCode}";
                statusAssertion.ExpectedValue = "2xx Success";
                statusAssertion.ErrorMessage = $"Expected successful status code, got {response.StatusCode}";
            }

            testCase.Assertions.Add(statusAssertion);

            // Response time assertion
            if (configuration.ValidateResponseTime && testCase.ResponseTimeMs.HasValue)
            {
                var responseTimeAssertion = new TestAssertion
                {
                    Name = "Response Time",
                    Description = $"Response should be under {configuration.MaxResponseTimeMs}ms",
                    ActualValue = $"{testCase.ResponseTimeMs}ms",
                    ExpectedValue = $"< {configuration.MaxResponseTimeMs}ms"
                };

                responseTimeAssertion.Passed = testCase.ResponseTimeMs.Value < configuration.MaxResponseTimeMs;
                if (!responseTimeAssertion.Passed)
                {
                    responseTimeAssertion.ErrorMessage = $"Response time {testCase.ResponseTimeMs}ms exceeds maximum {configuration.MaxResponseTimeMs}ms";
                }

                testCase.Assertions.Add(responseTimeAssertion);
            }

            // Content type assertion
            if (response.IsSuccessStatusCode)
            {
                var contentTypeAssertion = new TestAssertion
                {
                    Name = "Content Type",
                    Description = "Response should be JSON",
                    ExpectedValue = "application/json"
                };

                var contentType = response.Content.Headers.ContentType?.MediaType;
                contentTypeAssertion.ActualValue = contentType ?? "unknown";
                contentTypeAssertion.Passed = contentType != null && contentType.Contains("json", StringComparison.OrdinalIgnoreCase);

                if (!contentTypeAssertion.Passed)
                {
                    contentTypeAssertion.ErrorMessage = $"Expected JSON content type, got {contentType}";
                }

                testCase.Assertions.Add(contentTypeAssertion);

                // JSON structure validation
                if (!string.IsNullOrEmpty(testCase.ResponseBody))
                {
                    var jsonAssertion = new TestAssertion
                    {
                        Name = "Valid JSON",
                        Description = "Response should be valid JSON"
                    };

                    try
                    {
                        JsonDocument.Parse(testCase.ResponseBody);
                        jsonAssertion.Passed = true;
                        jsonAssertion.ActualValue = "Valid JSON";
                        jsonAssertion.ExpectedValue = "Valid JSON";
                    }
                    catch (JsonException ex)
                    {
                        jsonAssertion.Passed = false;
                        jsonAssertion.ActualValue = "Invalid JSON";
                        jsonAssertion.ExpectedValue = "Valid JSON";
                        jsonAssertion.ErrorMessage = $"Invalid JSON: {ex.Message}";
                    }

                    testCase.Assertions.Add(jsonAssertion);
                }
            }

            await Task.CompletedTask;
        }

        private string GenerateRequestBody(ApiEndpoint endpoint)
        {
            if (endpoint.Path.Contains("/quotation", StringComparison.OrdinalIgnoreCase))
            {
                return JsonSerializer.Serialize(new
                {
                    address = "Gran Via Street 1, Madrid",
                    client = "test-client",
                    service = "Capacity",
                    carrier = "test-carrier",
                    capacityMbps = 100,
                    termMonths = 36,
                    offNetOLO = false,
                    CIDR = 30,
                    requestID = "TEST-001"
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }

            return "{}";
        }

        private List<TestCase> GenerateNegativeTestCases(ApiEndpoint endpoint)
        {
            var negativeTests = new List<TestCase>();

            if (endpoint.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                // Missing required parameters test
                negativeTests.Add(new TestCase
                {
                    Id = Guid.NewGuid(),
                    Name = $"{endpoint.Method} {endpoint.Path} - Missing Required Fields",
                    Description = "Test request with missing required parameters",
                    Method = endpoint.Method,
                    Endpoint = endpoint.Path,
                    RequestBody = "{}",
                    Status = TestStatus.Pending
                });

                // Invalid data types test
                negativeTests.Add(new TestCase
                {
                    Id = Guid.NewGuid(),
                    Name = $"{endpoint.Method} {endpoint.Path} - Invalid Data Types",
                    Description = "Test request with invalid data types",
                    Method = endpoint.Method,
                    Endpoint = endpoint.Path,
                    RequestBody = JsonSerializer.Serialize(new
                    {
                        address = "Gran Via Street 1, Madrid",
                        client = "test-client",
                        service = "Capacity",
                        carrier = "test-carrier",
                        capacityMbps = "invalid-number", // Invalid type
                        termMonths = 36,
                        requestID = "TEST-002"
                    }),
                    Status = TestStatus.Pending
                });
            }

            return negativeTests;
        }
    }
}