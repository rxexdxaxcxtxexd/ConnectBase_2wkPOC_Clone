using ColteBondingAPI.Infrastructure.Authentication;
using ColteBondingAPI.Models.Requests;
using ColteBondingAPI.Models.Responses;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ColteBondingAPI.Infrastructure.Clients;

public interface IColtApiClient
{
    Task<AvailabilityResponse> CheckAvailabilityAsync(AvailabilityCheckRequest request);
    Task<LocationAvailability> GetLocationAvailabilityAsync(string locationId);
    Task<ServiceabilityDetails> GetServiceabilityDetailsAsync(string locationId, string serviceType);
    Task<QuoteResponse> CreateQuoteAsync(CreateQuoteRequest request);
    Task<QuoteResponse> GetQuoteAsync(string quoteId);
    Task<OrderResponse> SubmitOrderAsync(SubmitOrderRequest request);
    Task<OrderStatusResponse> GetOrderStatusAsync(string orderId);
}

public class ColtApiClient : IColtApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;
    private readonly ColtApiSettings _settings;
    private readonly ILogger<ColtApiClient> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public ColtApiClient(
        HttpClient httpClient,
        ITokenService tokenService,
        IOptions<ColtApiSettings> settings,
        ILogger<ColtApiClient> logger)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _settings = settings.Value;
        _logger = logger;

        // Configure retry policy with circuit breaker
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                _settings.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} after {Timespan}s", retryCount, timespan.TotalSeconds);
                })
            .WrapAsync(Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .CircuitBreakerAsync(
                    _settings.CircuitBreakerThreshold,
                    TimeSpan.FromMinutes(1),
                    onBreak: (result, timespan) =>
                    {
                        _logger.LogError("Circuit breaker opened for {Timespan}s", timespan.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset");
                    }));
    }

    public async Task<AvailabilityResponse> CheckAvailabilityAsync(AvailabilityCheckRequest request)
    {
        try
        {
            var response = await SendRequestAsync<AvailabilityResponse>(
                HttpMethod.Post,
                "/availability/check",
                request);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability");
            throw new ColtApiException("Failed to check availability", ex);
        }
    }

    public async Task<LocationAvailability> GetLocationAvailabilityAsync(string locationId)
    {
        try
        {
            var response = await SendRequestAsync<LocationAvailability>(
                HttpMethod.Get,
                $"/availability/{locationId}");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location availability for {LocationId}", locationId);
            throw new ColtApiException($"Failed to get location availability for {locationId}", ex);
        }
    }

    public async Task<ServiceabilityDetails> GetServiceabilityDetailsAsync(string locationId, string serviceType)
    {
        try
        {
            var response = await SendRequestAsync<ServiceabilityDetails>(
                HttpMethod.Get,
                $"/serviceability/{locationId}/{serviceType}");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serviceability details");
            throw new ColtApiException("Failed to get serviceability details", ex);
        }
    }

    public async Task<QuoteResponse> CreateQuoteAsync(CreateQuoteRequest request)
    {
        try
        {
            var response = await SendRequestAsync<QuoteResponse>(
                HttpMethod.Post,
                "/quotes/create",
                request);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quote");
            throw new ColtApiException("Failed to create quote", ex);
        }
    }

    public async Task<QuoteResponse> GetQuoteAsync(string quoteId)
    {
        try
        {
            var response = await SendRequestAsync<QuoteResponse>(
                HttpMethod.Get,
                $"/quotes/{quoteId}");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quote {QuoteId}", quoteId);
            throw new ColtApiException($"Failed to get quote {quoteId}", ex);
        }
    }

    public async Task<OrderResponse> SubmitOrderAsync(SubmitOrderRequest request)
    {
        try
        {
            var response = await SendRequestAsync<OrderResponse>(
                HttpMethod.Post,
                "/orders/submit",
                request);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting order");
            throw new ColtApiException("Failed to submit order", ex);
        }
    }

    public async Task<OrderStatusResponse> GetOrderStatusAsync(string orderId)
    {
        try
        {
            var response = await SendRequestAsync<OrderStatusResponse>(
                HttpMethod.Get,
                $"/orders/{orderId}/status");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order status for {OrderId}", orderId);
            throw new ColtApiException($"Failed to get order status for {orderId}", ex);
        }
    }

    private async Task<T> SendRequestAsync<T>(HttpMethod method, string endpoint, object content = null)
    {
        var request = new HttpRequestMessage(method, $"{_settings.BaseUrl}{endpoint}");
        
        // Add authentication
        var token = await _tokenService.GetTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Add request ID for tracing
        var requestId = Guid.NewGuid().ToString();
        request.Headers.Add("X-Request-Id", requestId);
        
        // Add content if provided
        if (content != null)
        {
            var json = JsonSerializer.Serialize(content);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        _logger.LogInformation("Sending {Method} request to {Endpoint} with RequestId {RequestId}", 
            method, endpoint, requestId);

        // Execute with retry policy
        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            // Clone the request for retries
            var clonedRequest = await CloneHttpRequestMessageAsync(request);
            return await _httpClient.SendAsync(clonedRequest);
        });

        // Log response
        _logger.LogInformation("Received response with status {StatusCode} for RequestId {RequestId}", 
            response.StatusCode, requestId);

        // Handle rate limiting
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 60;
            _logger.LogWarning("Rate limit exceeded. Retry after {RetryAfter} seconds", retryAfter);
            throw new RateLimitExceededException($"Rate limit exceeded. Retry after {retryAfter} seconds");
        }

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return default(T);
        }

        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content != null)
        {
            var contentString = await request.Content.ReadAsStringAsync();
            clone.Content = new StringContent(contentString, Encoding.UTF8, "application/json");
        }

        return clone;
    }
}

public class ColtApiException : Exception
{
    public ColtApiException(string message) : base(message) { }
    public ColtApiException(string message, Exception innerException) : base(message, innerException) { }
}

public class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message) : base(message) { }
}