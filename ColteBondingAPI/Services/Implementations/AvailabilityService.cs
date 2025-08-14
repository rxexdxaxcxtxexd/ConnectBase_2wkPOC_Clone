using ColteBondingAPI.Models.Requests;
using ColteBondingAPI.Models.Responses;
using ColteBondingAPI.Services.Interfaces;
using ColteBondingAPI.Infrastructure.Clients;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ColteBondingAPI.Services.Implementations;

public class AvailabilityService : IAvailabilityService
{
    private readonly IColtApiClient _coltApiClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AvailabilityService> _logger;
    private const int CacheExpirationMinutes = 30;

    public AvailabilityService(
        IColtApiClient coltApiClient,
        IDistributedCache cache,
        ILogger<AvailabilityService> logger)
    {
        _coltApiClient = coltApiClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AvailabilityResponse> CheckAvailabilityAsync(AvailabilityCheckRequest request)
    {
        try
        {
            // Check cache first
            var cacheKey = GenerateCacheKey(request);
            var cachedResult = await GetFromCacheAsync<AvailabilityResponse>(cacheKey);
            
            if (cachedResult != null)
            {
                _logger.LogInformation("Returning cached availability result");
                return cachedResult;
            }

            // Validate request
            await ValidateRequestAsync(request);

            // Call Colt API
            var response = await _coltApiClient.CheckAvailabilityAsync(request);

            // Cache the result
            await SetCacheAsync(cacheKey, response, TimeSpan.FromMinutes(CacheExpirationMinutes));

            // Log metrics
            LogAvailabilityMetrics(response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability");
            throw;
        }
    }

    public async Task<LocationAvailability> GetLocationAvailabilityAsync(string locationId)
    {
        try
        {
            var cacheKey = $"location:{locationId}";
            var cachedResult = await GetFromCacheAsync<LocationAvailability>(cacheKey);
            
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _coltApiClient.GetLocationAvailabilityAsync(locationId);
            
            if (result != null)
            {
                await SetCacheAsync(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location availability for {LocationId}", locationId);
            throw;
        }
    }

    public async Task<BatchAvailabilityResponse> BatchCheckAvailabilityAsync(BatchAvailabilityRequest request)
    {
        try
        {
            var tasks = new List<Task<AvailabilityResponse>>();
            
            foreach (var availabilityRequest in request.Requests)
            {
                tasks.Add(CheckAvailabilityAsync(availabilityRequest));
            }

            var results = await Task.WhenAll(tasks);

            return new BatchAvailabilityResponse
            {
                RequestId = Guid.NewGuid().ToString(),
                Results = results.ToList(),
                ProcessedAt = DateTime.UtcNow,
                TotalRequests = request.Requests.Count,
                SuccessfulRequests = results.Count(r => r != null)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch availability check");
            throw;
        }
    }

    public async Task<bool> ValidateLocationAsync(Location location)
    {
        try
        {
            // Validate location format
            if (location == null)
            {
                return false;
            }

            if (location.Type == LocationType.Address)
            {
                return !string.IsNullOrWhiteSpace(location.Address?.StreetAddress) &&
                       !string.IsNullOrWhiteSpace(location.Address?.City) &&
                       !string.IsNullOrWhiteSpace(location.Address?.Country);
            }
            else if (location.Type == LocationType.Coordinates)
            {
                return location.Coordinates != null &&
                       location.Coordinates.Latitude >= -90 && location.Coordinates.Latitude <= 90 &&
                       location.Coordinates.Longitude >= -180 && location.Coordinates.Longitude <= 180;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating location");
            return false;
        }
    }

    public async Task<ServiceabilityDetails> GetServiceabilityDetailsAsync(string locationId, string serviceType)
    {
        try
        {
            var cacheKey = $"serviceability:{locationId}:{serviceType}";
            var cachedResult = await GetFromCacheAsync<ServiceabilityDetails>(cacheKey);
            
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _coltApiClient.GetServiceabilityDetailsAsync(locationId, serviceType);
            
            if (result != null)
            {
                await SetCacheAsync(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes * 2));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serviceability details for {LocationId} {ServiceType}", 
                locationId, serviceType);
            throw;
        }
    }

    private async Task ValidateRequestAsync(AvailabilityCheckRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Locations == null || !request.Locations.Any())
        {
            throw new ArgumentException("At least one location is required");
        }

        foreach (var location in request.Locations)
        {
            var isValid = await ValidateLocationAsync(location);
            if (!isValid)
            {
                throw new ArgumentException($"Invalid location: {JsonSerializer.Serialize(location)}");
            }
        }

        if (string.IsNullOrWhiteSpace(request.ServiceType))
        {
            throw new ArgumentException("Service type is required");
        }

        if (request.Bandwidth <= 0)
        {
            throw new ArgumentException("Bandwidth must be greater than 0");
        }
    }

    private string GenerateCacheKey(AvailabilityCheckRequest request)
    {
        var locations = string.Join("-", request.Locations.Select(l => 
            l.Type == LocationType.Address 
                ? $"{l.Address?.StreetAddress}{l.Address?.City}{l.Address?.PostalCode}"
                : $"{l.Coordinates?.Latitude}{l.Coordinates?.Longitude}"));
        
        return $"availability:{locations}:{request.ServiceType}:{request.Bandwidth}";
    }

    private async Task<T> GetFromCacheAsync<T>(string key)
    {
        try
        {
            var cached = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<T>(cached);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from cache for key {Key}", key);
        }

        return default(T);
    }

    private async Task SetCacheAsync<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            var serialized = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serialized, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error writing to cache for key {Key}", key);
        }
    }

    private void LogAvailabilityMetrics(AvailabilityResponse response)
    {
        var serviceableCount = response.Availability.Count(a => a.Serviceable);
        var onNetCount = response.Availability.Count(a => a.OnNet);
        
        _logger.LogInformation(
            "Availability check completed. Total: {Total}, Serviceable: {Serviceable}, OnNet: {OnNet}",
            response.Availability.Count,
            serviceableCount,
            onNetCount);
    }
}