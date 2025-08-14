using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ColteBondingAPI.Infrastructure.Authentication;

public interface ITokenService
{
    Task<string> GetTokenAsync();
    Task<bool> ValidateTokenAsync(string token);
    void InvalidateToken();
}

public class TokenService : ITokenService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ColtApiSettings _settings;
    private readonly ILogger<TokenService> _logger;
    private const string TokenCacheKey = "colt_api_token";
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public TokenService(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ColtApiSettings> settings,
        ILogger<TokenService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync()
    {
        // Try to get token from cache
        if (_cache.TryGetValue(TokenCacheKey, out string cachedToken))
        {
            if (await ValidateTokenAsync(cachedToken))
            {
                return cachedToken;
            }
        }

        // Use semaphore to prevent multiple simultaneous token requests
        await _semaphore.WaitAsync();
        try
        {
            // Double-check cache after acquiring semaphore
            if (_cache.TryGetValue(TokenCacheKey, out cachedToken))
            {
                if (await ValidateTokenAsync(cachedToken))
                {
                    return cachedToken;
                }
            }

            // Request new token
            var token = await RequestNewTokenAsync();
            
            // Cache the token with a sliding expiration
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(55)) // Token expires after 60 minutes, refresh at 55
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(59));

            _cache.Set(TokenCacheKey, token, cacheOptions);
            
            _logger.LogInformation("New token obtained and cached");
            
            return token;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            // Parse JWT token to check expiration
            var tokenParts = token.Split('.');
            if (tokenParts.Length != 3)
            {
                return false;
            }

            var payload = tokenParts[1];
            var paddedPayload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var payloadBytes = Convert.FromBase64String(paddedPayload);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            
            using var doc = JsonDocument.Parse(payloadJson);
            
            if (doc.RootElement.TryGetProperty("exp", out var expElement))
            {
                var exp = expElement.GetInt64();
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                
                // Check if token expires in less than 5 minutes
                var timeUntilExpiration = expirationTime - DateTimeOffset.UtcNow;
                
                if (timeUntilExpiration.TotalMinutes < 5)
                {
                    _logger.LogInformation("Token expires in {Minutes} minutes, will refresh", 
                        timeUntilExpiration.TotalMinutes);
                    return false;
                }
                
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating token");
            return false;
        }
    }

    public void InvalidateToken()
    {
        _cache.Remove(TokenCacheKey);
        _logger.LogInformation("Token invalidated");
    }

    private async Task<string> RequestNewTokenAsync()
    {
        try
        {
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _settings.ClientId),
                new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
                new KeyValuePair<string, string>("scope", "ebonding.read ebonding.write")
            });

            _logger.LogInformation("Requesting new token from {TokenUrl}", _settings.TokenUrl);

            var response = await _httpClient.PostAsync(_settings.TokenUrl, tokenRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token request failed with status {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                throw new AuthenticationException($"Failed to obtain token: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            
            if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
            {
                return tokenElement.GetString();
            }

            throw new AuthenticationException("Token response did not contain access_token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting new token");
            throw new AuthenticationException("Failed to obtain authentication token", ex);
        }
    }
}

public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
    public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}