using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TelecomApiAnalyzer.Web.Models;

namespace TelecomApiAnalyzer.Web.Services
{
    public class OptusApiService : IOptusApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OptusApiService> _logger;
        private readonly OptusApiSettings _settings;

        public OptusApiService(HttpClient httpClient, ILogger<OptusApiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = configuration.GetSection("OptusApiSettings").Get<OptusApiSettings>() ?? new OptusApiSettings();

            ConfigureHttpClient();
        }

        public async Task<OptusB2BSQResponse> ServiceQualificationAsync(OptusB2BSQParams parameters, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting OPTUS B2B-SQ service qualification request");

                // Convert parameters to form-encoded string
                var formContent = CreateFormContent(new Dictionary<string, string>
                {
                    { "serviceAddress", parameters.ServiceAddress ?? "" },
                    { "postCode", parameters.PostCode ?? "" },
                    { "state", parameters.State ?? "" },
                    { "serviceType", parameters.ServiceType ?? "NBN" },
                    { "bandwidth", parameters.Bandwidth ?? "100" },
                    { "customerId", parameters.CustomerId ?? "B2BNitel" },
                    { "requestId", parameters.RequestId ?? Guid.NewGuid().ToString() }
                });

                var encodedParams = await formContent.ReadAsStringAsync(cancellationToken);
                var wrappedContent = new StringContent($"Param={Uri.EscapeDataString(encodedParams)}", Encoding.UTF8, _settings.ContentType);

                var requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}{_settings.B2BSQEndpoint}";
                _logger.LogInformation("Sending B2B-SQ request to: {Url}", requestUrl);

                var response = await _httpClient.PostAsync(requestUrl, wrappedContent, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation("B2B-SQ response status: {StatusCode}", response.StatusCode);
                _logger.LogDebug("B2B-SQ response content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        return JsonSerializer.Deserialize<OptusB2BSQResponse>(responseContent) ?? new OptusB2BSQResponse
                        {
                            Status = "Success",
                            Message = "Service qualification completed",
                            Timestamp = DateTime.UtcNow,
                            Data = new OptusB2BSQData
                            {
                                ServiceQualificationId = Guid.NewGuid().ToString(),
                                ServiceabilityStatus = "Available"
                            }
                        };
                    }
                    catch (JsonException)
                    {
                        // Handle non-JSON response
                        return new OptusB2BSQResponse
                        {
                            Status = "Success",
                            Message = responseContent,
                            Timestamp = DateTime.UtcNow,
                            RequestId = parameters.RequestId
                        };
                    }
                }
                else
                {
                    return new OptusB2BSQResponse
                    {
                        Status = "Error",
                        Message = $"HTTP {response.StatusCode}: {responseContent}",
                        Timestamp = DateTime.UtcNow,
                        RequestId = parameters.RequestId
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during B2B-SQ service qualification");
                return new OptusB2BSQResponse
                {
                    Status = "Error",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow,
                    RequestId = parameters.RequestId
                };
            }
        }

        public async Task<OptusB2BQuoteResponse> CreateQuoteAsync(OptusB2BQuoteParams parameters, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting OPTUS B2B-QUOTE request");

                // Convert parameters to form-encoded string
                var formContent = CreateFormContent(new Dictionary<string, string>
                {
                    { "serviceQualificationId", parameters.ServiceQualificationId ?? Guid.NewGuid().ToString() },
                    { "productId", parameters.ProductId ?? "NBN-100" },
                    { "customerId", parameters.CustomerId ?? "B2BNitel" },
                    { "customerName", parameters.CustomerName ?? "Nitel Communications" },
                    { "serviceAddress", parameters.ServiceAddress ?? "Test Address" },
                    { "contactEmail", parameters.ContactEmail ?? "test@nitel.com" },
                    { "contactPhone", parameters.ContactPhone ?? "+61400000000" },
                    { "requestedDeliveryDate", parameters.RequestedDeliveryDate ?? DateTime.Now.AddDays(30).ToString("yyyy-MM-dd") },
                    { "contractTerm", parameters.ContractTerm ?? "24" },
                    { "requestId", parameters.RequestId ?? Guid.NewGuid().ToString() }
                });

                var encodedParams = await formContent.ReadAsStringAsync(cancellationToken);
                var wrappedContent = new StringContent($"Param={Uri.EscapeDataString(encodedParams)}", Encoding.UTF8, _settings.ContentType);

                var requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}{_settings.B2BQuoteEndpoint}";
                _logger.LogInformation("Sending B2B-QUOTE request to: {Url}", requestUrl);

                var response = await _httpClient.PostAsync(requestUrl, wrappedContent, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation("B2B-QUOTE response status: {StatusCode}", response.StatusCode);
                _logger.LogDebug("B2B-QUOTE response content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        return JsonSerializer.Deserialize<OptusB2BQuoteResponse>(responseContent) ?? new OptusB2BQuoteResponse
                        {
                            Status = "Success",
                            Message = "Quote created successfully",
                            Timestamp = DateTime.UtcNow,
                            QuoteId = Guid.NewGuid().ToString(),
                            Data = new OptusB2BQuoteData
                            {
                                QuoteNumber = $"QTE-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                                QuoteStatus = "Active",
                                TotalPrice = 99.95m,
                                Currency = "AUD",
                                ValidUntil = DateTime.UtcNow.AddDays(30)
                            }
                        };
                    }
                    catch (JsonException)
                    {
                        // Handle non-JSON response
                        return new OptusB2BQuoteResponse
                        {
                            Status = "Success",
                            Message = responseContent,
                            Timestamp = DateTime.UtcNow,
                            QuoteId = parameters.RequestId
                        };
                    }
                }
                else
                {
                    return new OptusB2BQuoteResponse
                    {
                        Status = "Error",
                        Message = $"HTTP {response.StatusCode}: {responseContent}",
                        Timestamp = DateTime.UtcNow,
                        QuoteId = parameters.RequestId
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during B2B-QUOTE creation");
                return new OptusB2BQuoteResponse
                {
                    Status = "Error",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow,
                    QuoteId = parameters.RequestId
                };
            }
        }

        public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Validating OPTUS API connection");

                // Simple validation request to check if the endpoint is reachable
                var response = await _httpClient.GetAsync(_settings.BaseUrl, cancellationToken);
                
                _logger.LogInformation("Connection validation response: {StatusCode}", response.StatusCode);
                return response.StatusCode != System.Net.HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Connection validation failed");
                return false;
            }
        }

        public async Task<string> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var isConnected = await ValidateConnectionAsync(cancellationToken);
                return isConnected ? "Connected" : "Disconnected";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health status check failed");
                return "Error";
            }
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Add basic authentication if credentials are provided
            if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.Username}:{_settings.Password}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            }

            // Set common headers
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelecomApiAnalyzer/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        }

        private static FormUrlEncodedContent CreateFormContent(Dictionary<string, string> parameters)
        {
            var keyValuePairs = new List<KeyValuePair<string, string>>();
            foreach (var param in parameters)
            {
                keyValuePairs.Add(new KeyValuePair<string, string>(param.Key, param.Value));
            }
            return new FormUrlEncodedContent(keyValuePairs);
        }
    }
}