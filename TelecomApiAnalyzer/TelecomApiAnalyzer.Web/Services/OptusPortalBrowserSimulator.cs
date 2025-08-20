using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelecomApiAnalyzer.Web.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace TelecomApiAnalyzer.Web.Services
{
    /// <summary>
    /// Browser simulation service for complex OPTUS portal workflows
    /// Handles multi-step authentication, form submissions, and session management
    /// </summary>
    public class OptusPortalBrowserSimulator
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OptusPortalBrowserSimulator> _logger;
        private readonly CookieContainer _cookieContainer;
        private OptusPortalSession? _currentSession;

        public OptusPortalBrowserSimulator(HttpClient httpClient, ILogger<OptusPortalBrowserSimulator> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cookieContainer = new CookieContainer();
            
            ConfigureBrowserLikeClient();
        }

        /// <summary>
        /// Simulates complete browser workflow for OPTUS portal authentication and operations
        /// </summary>
        public async Task<OptusPortalWorkflowResult> ExecuteCompleteWorkflowAsync(OptusPortalWorkflowRequest request, CancellationToken cancellationToken = default)
        {
            var result = new OptusPortalWorkflowResult
            {
                WorkflowId = Guid.NewGuid(),
                StartedAt = DateTime.UtcNow,
                Steps = new List<OptusPortalWorkflowStep>()
            };

            try
            {
                _logger.LogInformation("Starting complete portal workflow: {WorkflowType}", request.WorkflowType);

                // Step 1: Initialize session and get login page
                var loginStep = await ExecuteLoginPageAccessStep(cancellationToken);
                result.Steps.Add(loginStep);

                if (!loginStep.IsCompleted)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to access login page";
                    return result;
                }

                // Step 2: Perform authentication
                var authStep = await ExecuteAuthenticationStep(request.Username, request.Password, cancellationToken);
                result.Steps.Add(authStep);

                if (!authStep.IsCompleted)
                {
                    result.Success = false;
                    result.ErrorMessage = "Authentication failed";
                    return result;
                }

                // Step 3: Navigate to main dashboard
                var dashboardStep = await ExecuteDashboardAccessStep(cancellationToken);
                result.Steps.Add(dashboardStep);

                // Step 4: Execute workflow-specific operations
                switch (request.WorkflowType.ToLower())
                {
                    case "quotation":
                        var quoteStep = await ExecuteQuotationWorkflowStep(request.QuotationData, cancellationToken);
                        result.Steps.Add(quoteStep);
                        break;
                    case "carrier-lookup":
                        var carrierStep = await ExecuteCarrierLookupStep(cancellationToken);
                        result.Steps.Add(carrierStep);
                        break;
                    case "service-availability":
                        var availabilityStep = await ExecuteServiceAvailabilityStep(request.ServiceAddress, cancellationToken);
                        result.Steps.Add(availabilityStep);
                        break;
                }

                result.CompletedAt = DateTime.UtcNow;
                result.Success = result.Steps.TrueForAll(s => s.IsCompleted);
                result.SessionId = _currentSession?.SessionId;

                _logger.LogInformation("Workflow completed: {Success}, Duration: {Duration}ms",
                    result.Success, (result.CompletedAt.Value - result.StartedAt).TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing portal workflow");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }
        }

        /// <summary>
        /// Simulates complete quotation creation workflow
        /// </summary>
        public async Task<OptusPortalQuoteResult> CreateQuotationWithBrowserSimulationAsync(OptusPortalQuoteRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Simulating browser-based quotation creation");

                // Ensure we have an active session
                if (_currentSession == null || _currentSession.IsExpired)
                {
                    throw new InvalidOperationException("No active session. Must authenticate first.");
                }

                // Step 1: Navigate to quote creation page
                var quotePageResponse = await NavigateToPageWithRetry("/Quote/Create", cancellationToken);
                if (!quotePageResponse.IsSuccessStatusCode)
                {
                    return new OptusPortalQuoteResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to access quote creation page",
                        StatusCode = (int)quotePageResponse.StatusCode
                    };
                }

                var quotePageHtml = await quotePageResponse.Content.ReadAsStringAsync(cancellationToken);

                // Step 2: Parse form and prepare submission data
                var formData = await PrepareQuoteFormWithBrowserLogic(request, quotePageHtml);

                // Step 3: Submit form with browser-like behavior
                var submitResult = await SubmitFormWithBrowserSimulation("/Quote/Submit", formData, cancellationToken);

                // Step 4: Parse response and extract quote information
                return await ParseQuoteSubmissionResponse(submitResult, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in browser-simulated quotation creation");
                return new OptusPortalQuoteResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Details = "Browser simulation failed"
                };
            }
        }

        /// <summary>
        /// Maintains session state and handles automatic re-authentication
        /// </summary>
        public async Task<bool> MaintainSessionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_currentSession == null)
                    return false;

                // Check if session is still valid
                var healthResponse = await _httpClient.GetAsync("/default.aspx", cancellationToken);
                var healthContent = await healthResponse.Content.ReadAsStringAsync(cancellationToken);

                // If redirected to login page, session expired
                if (healthContent.Contains("login", StringComparison.OrdinalIgnoreCase) &&
                    healthContent.Contains("password", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Session expired, clearing session state");
                    _currentSession = null;
                    return false;
                }

                // Update session activity
                _currentSession.RefreshActivity();
                _logger.LogDebug("Session maintained successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error maintaining session");
                return false;
            }
        }

        private async Task<OptusPortalWorkflowStep> ExecuteLoginPageAccessStep(CancellationToken cancellationToken)
        {
            var step = new OptusPortalWorkflowStep
            {
                Name = "Access Login Page",
                Url = "/Login.aspx",
                Description = "Navigate to OPTUS portal login page and initialize session",
                Order = 1
            };

            try
            {
                var response = await _httpClient.GetAsync("/Login.aspx", cancellationToken);
                var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    // Initialize session
                    _currentSession = new OptusPortalSession
                    {
                        SessionId = ExtractSessionId(htmlContent) ?? Guid.NewGuid().ToString(),
                        CsrfToken = ExtractCsrfToken(htmlContent)
                    };

                    step.IsCompleted = true;
                    step.StepData["csrf_token"] = _currentSession.CsrfToken ?? "";
                    step.StepData["session_id"] = _currentSession.SessionId;
                    step.StepData["login_form_found"] = htmlContent.Contains("form", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    step.IsCompleted = false;
                    step.StepData["error"] = $"HTTP {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                step.IsCompleted = false;
                step.StepData["error"] = ex.Message;
            }

            return step;
        }

        private async Task<OptusPortalWorkflowStep> ExecuteAuthenticationStep(string username, string password, CancellationToken cancellationToken)
        {
            var step = new OptusPortalWorkflowStep
            {
                Name = "Portal Authentication",
                Url = "/Login.aspx",
                Description = "Submit login credentials to OPTUS portal",
                Order = 2
            };

            try
            {
                var formData = new List<KeyValuePair<string, string>>
                {
                    new("username", username),
                    new("password", password)
                };

                if (!string.IsNullOrEmpty(_currentSession?.CsrfToken))
                {
                    formData.Add(new KeyValuePair<string, string>("__RequestVerificationToken", _currentSession.CsrfToken));
                }

                var formContent = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync("/Login.aspx", formContent, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                // Determine success based on response content and status
                var isSuccess = response.IsSuccessStatusCode &&
                              !responseContent.Contains("error", StringComparison.OrdinalIgnoreCase) &&
                              !responseContent.Contains("invalid", StringComparison.OrdinalIgnoreCase);

                step.IsCompleted = isSuccess;
                step.StepData["authentication_result"] = isSuccess ? "Success" : "Failed";
                step.StepData["redirect_detected"] = response.Headers.Location != null;

                if (isSuccess && _currentSession != null)
                {
                    _currentSession.IsAuthenticated = true;
                    _currentSession.Username = username;
                }
            }
            catch (Exception ex)
            {
                step.IsCompleted = false;
                step.StepData["error"] = ex.Message;
            }

            return step;
        }

        private async Task<OptusPortalWorkflowStep> ExecuteDashboardAccessStep(CancellationToken cancellationToken)
        {
            var step = new OptusPortalWorkflowStep
            {
                Name = "Dashboard Access",
                Url = "/default.aspx",
                Description = "Navigate to main portal dashboard",
                Order = 3
            };

            try
            {
                var response = await _httpClient.GetAsync("/default.aspx", cancellationToken);
                var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);

                step.IsCompleted = response.IsSuccessStatusCode &&
                                  !htmlContent.Contains("login", StringComparison.OrdinalIgnoreCase);
                step.StepData["dashboard_loaded"] = step.IsCompleted;
                step.StepData["page_title"] = ExtractPageTitle(htmlContent);
                step.StepData["navigation_links"] = CountNavigationLinks(htmlContent);
            }
            catch (Exception ex)
            {
                step.IsCompleted = false;
                step.StepData["error"] = ex.Message;
            }

            return step;
        }

        private async Task<OptusPortalWorkflowStep> ExecuteQuotationWorkflowStep(OptusPortalQuoteRequest? quotationData, CancellationToken cancellationToken)
        {
            var step = new OptusPortalWorkflowStep
            {
                Name = "Quotation Creation",
                Url = "/Quote/Create",
                Description = "Navigate to quote creation and simulate form submission",
                Order = 4
            };

            if (quotationData == null)
            {
                step.IsCompleted = false;
                step.StepData["error"] = "No quotation data provided";
                return step;
            }

            try
            {
                var quoteResult = await CreateQuotationWithBrowserSimulationAsync(quotationData, cancellationToken);
                step.IsCompleted = quoteResult.Success;
                step.StepData["quote_id"] = quoteResult.QuoteId ?? "";
                step.StepData["quote_result"] = quoteResult.Success ? "Created" : "Failed";

                if (!quoteResult.Success)
                {
                    step.StepData["error"] = quoteResult.ErrorMessage ?? "Unknown error";
                }
            }
            catch (Exception ex)
            {
                step.IsCompleted = false;
                step.StepData["error"] = ex.Message;
            }

            return step;
        }

        private async Task<OptusPortalWorkflowStep> ExecuteCarrierLookupStep(CancellationToken cancellationToken)
        {
            var step = new OptusPortalWorkflowStep
            {
                Name = "Carrier Lookup",
                Url = "/Carriers",
                Description = "Retrieve available carrier information",
                Order = 4
            };

            try
            {
                var response = await NavigateToPageWithRetry("/default.aspx", cancellationToken);
                var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);

                // Simulate carrier data extraction from dashboard or dedicated page
                var carrierCount = CountCarrierReferences(htmlContent);
                
                step.IsCompleted = response.IsSuccessStatusCode;
                step.StepData["carriers_found"] = carrierCount;
                step.StepData["has_carrier_data"] = carrierCount > 0;
            }
            catch (Exception ex)
            {
                step.IsCompleted = false;
                step.StepData["error"] = ex.Message;
            }

            return step;
        }

        private async Task<OptusPortalWorkflowStep> ExecuteServiceAvailabilityStep(string? serviceAddress, CancellationToken cancellationToken)
        {
            var step = new OptusPortalWorkflowStep
            {
                Name = "Service Availability Check",
                Url = "/Services/Availability",
                Description = "Check service availability for specified address",
                Order = 4
            };

            if (string.IsNullOrEmpty(serviceAddress))
            {
                step.IsCompleted = false;
                step.StepData["error"] = "No service address provided";
                return step;
            }

            try
            {
                // Simulate service availability check
                var response = await _httpClient.GetAsync($"/default.aspx?address={Uri.EscapeDataString(serviceAddress)}", cancellationToken);
                
                step.IsCompleted = response.IsSuccessStatusCode;
                step.StepData["address_checked"] = serviceAddress;
                step.StepData["availability_result"] = response.IsSuccessStatusCode ? "Available" : "Check Failed";
            }
            catch (Exception ex)
            {
                step.IsCompleted = false;
                step.StepData["error"] = ex.Message;
            }

            return step;
        }

        private void ConfigureBrowserLikeClient()
        {
            // Configure HTTP client to behave like a real browser
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _httpClient.DefaultRequestHeaders.Add("DNT", "1");
            _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        }

        private async Task<HttpResponseMessage> NavigateToPageWithRetry(string url, CancellationToken cancellationToken, int maxRetries = 3)
        {
            Exception? lastException = null;
            
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url, cancellationToken);
                    
                    // If session expired, attempt to maintain session
                    if (!response.IsSuccessStatusCode || response.Headers.Location?.ToString().Contains("login", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        await MaintainSessionAsync(cancellationToken);
                    }
                    
                    return response;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(1000 * (i + 1), cancellationToken); // Progressive backoff
                    }
                }
            }
            
            throw lastException ?? new InvalidOperationException("Failed to navigate to page");
        }

        private async Task<Dictionary<string, string>> PrepareQuoteFormWithBrowserLogic(OptusPortalQuoteRequest request, string htmlContent)
        {
            var formData = new Dictionary<string, string>
            {
                ["address"] = request.Address,
                ["client"] = request.Client,
                ["service"] = request.Service,
                ["carrier"] = request.Carrier,
                ["requestID"] = request.RequestID
            };

            // Add optional fields if provided
            if (request.CapacityMbps.HasValue)
                formData["capacityMbps"] = request.CapacityMbps.Value.ToString();
            if (request.TermMonths.HasValue)
                formData["termMonths"] = request.TermMonths.Value.ToString();
            if (request.CIDR.HasValue)
                formData["CIDR"] = request.CIDR.Value.ToString();
            if (request.OffNetOLO.HasValue)
                formData["offNetOLO"] = request.OffNetOLO.Value.ToString().ToLower();

            // Extract and add hidden form fields
            var hiddenFields = ExtractHiddenFormFields(htmlContent);
            foreach (var field in hiddenFields)
            {
                formData[field.Key] = field.Value;
            }

            // Add CSRF token if available
            if (!string.IsNullOrEmpty(_currentSession?.CsrfToken))
            {
                formData["__RequestVerificationToken"] = _currentSession.CsrfToken;
            }

            return await Task.FromResult(formData);
        }

        private async Task<HttpResponseMessage> SubmitFormWithBrowserSimulation(string submitUrl, Dictionary<string, string> formData, CancellationToken cancellationToken)
        {
            var formContent = new FormUrlEncodedContent(formData.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)));
            
            // Add form-specific headers
            var request = new HttpRequestMessage(HttpMethod.Post, submitUrl)
            {
                Content = formContent
            };
            
            request.Headers.Add("Referer", $"{_httpClient.BaseAddress}Quote/Create");
            request.Headers.Add("Origin", _httpClient.BaseAddress?.ToString().TrimEnd('/'));
            
            return await _httpClient.SendAsync(request, cancellationToken);
        }

        private async Task<OptusPortalQuoteResult> ParseQuoteSubmissionResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            return new OptusPortalQuoteResult
            {
                Success = response.IsSuccessStatusCode && !responseContent.Contains("error", StringComparison.OrdinalIgnoreCase),
                QuoteId = ExtractQuoteId(responseContent),
                HtmlResponse = responseContent,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = response.IsSuccessStatusCode ? null : "Form submission failed"
            };
        }

        // Helper methods for HTML parsing
        private string? ExtractSessionId(string htmlContent)
        {
            var match = Regex.Match(htmlContent, @"sessionId['""\\s]*[:=]['""\\s]*([A-Za-z0-9\\-_]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private string? ExtractCsrfToken(string htmlContent)
        {
            var match = Regex.Match(htmlContent, @"<input[^>]*name=['""]__RequestVerificationToken['""][^>]*value=['""]([^'""]*)['""]/?>", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private string? ExtractQuoteId(string htmlContent)
        {
            var match = Regex.Match(htmlContent, @"quote[Ii]d['""\\s]*[:=]['""\\s]*([A-Za-z0-9\\-_]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private string ExtractPageTitle(string htmlContent)
        {
            var match = Regex.Match(htmlContent, @"<title[^>]*>([^<]*)</title>", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private int CountNavigationLinks(string htmlContent)
        {
            var matches = Regex.Matches(htmlContent, @"<a[^>]*href[^>]*>", RegexOptions.IgnoreCase);
            return matches.Count;
        }

        private int CountCarrierReferences(string htmlContent)
        {
            var carrierPatterns = new[] { "carrier", "optus", "provider", "telco" };
            int count = 0;
            
            foreach (var pattern in carrierPatterns)
            {
                var matches = Regex.Matches(htmlContent, pattern, RegexOptions.IgnoreCase);
                count += matches.Count;
            }
            
            return count;
        }

        private Dictionary<string, string> ExtractHiddenFormFields(string htmlContent)
        {
            var hiddenFields = new Dictionary<string, string>();
            var matches = Regex.Matches(htmlContent, @"<input[^>]*type=['""]hidden['""][^>]*name=['""]([^'""]*)['""][^>]*value=['""]([^'""]*)['""][^>]*/?>");
            
            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                if (!string.IsNullOrEmpty(name))
                {
                    hiddenFields[name] = value;
                }
            }
            
            return hiddenFields;
        }
    }

    // Supporting models for workflow requests and results
    public class OptusPortalWorkflowRequest
    {
        public string WorkflowType { get; set; } = string.Empty; // "quotation", "carrier-lookup", "service-availability"
        public string Username { get; set; } = "B2BNitel";
        public string Password { get; set; } = string.Empty;
        public OptusPortalQuoteRequest? QuotationData { get; set; }
        public string? ServiceAddress { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class OptusPortalWorkflowResult
    {
        public Guid WorkflowId { get; set; }
        public bool Success { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<OptusPortalWorkflowStep> Steps { get; set; } = new();
        public string? SessionId { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : TimeSpan.Zero;
        public Dictionary<string, object> Results { get; set; } = new();
    }
}