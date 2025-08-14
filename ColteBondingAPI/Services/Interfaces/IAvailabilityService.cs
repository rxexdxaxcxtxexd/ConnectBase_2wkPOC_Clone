using ColteBondingAPI.Models.Requests;
using ColteBondingAPI.Models.Responses;

namespace ColteBondingAPI.Services.Interfaces;

public interface IAvailabilityService
{
    Task<AvailabilityResponse> CheckAvailabilityAsync(AvailabilityCheckRequest request);
    Task<LocationAvailability> GetLocationAvailabilityAsync(string locationId);
    Task<BatchAvailabilityResponse> BatchCheckAvailabilityAsync(BatchAvailabilityRequest request);
    Task<bool> ValidateLocationAsync(Location location);
    Task<ServiceabilityDetails> GetServiceabilityDetailsAsync(string locationId, string serviceType);
}