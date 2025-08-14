namespace ColteBondingAPI.Models.Responses;

public class AvailabilityResponse
{
    public string RequestId { get; set; }
    public List<LocationAvailability> Availability { get; set; }
    public DateTime Timestamp { get; set; }
}

public class LocationAvailability
{
    public string LocationId { get; set; }
    public bool Serviceable { get; set; }
    public bool OnNet { get; set; }
    public List<ServiceOption> Services { get; set; }
    public decimal? BuildCost { get; set; }
    public Address Address { get; set; }
    public Coordinates Coordinates { get; set; }
}

public class ServiceOption
{
    public string ServiceType { get; set; }
    public List<string> BandwidthOptions { get; set; }
    public string BandwidthUnit { get; set; }
    public LeadTime LeadTime { get; set; }
    public List<string> AvailableFeatures { get; set; }
}

public class LeadTime
{
    public int Standard { get; set; }
    public int? Expedited { get; set; }
    public string Unit { get; set; }
}

public class BatchAvailabilityResponse
{
    public string RequestId { get; set; }
    public List<AvailabilityResponse> Results { get; set; }
    public DateTime ProcessedAt { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
}

public class ServiceabilityDetails
{
    public string LocationId { get; set; }
    public string ServiceType { get; set; }
    public bool IsServiceable { get; set; }
    public ServiceCapabilities Capabilities { get; set; }
    public List<string> AvailableBandwidths { get; set; }
    public PricingIndicator PricingIndicator { get; set; }
    public NetworkDetails NetworkDetails { get; set; }
}

public class ServiceCapabilities
{
    public bool SupportsRedundancy { get; set; }
    public bool SupportsFlexBandwidth { get; set; }
    public bool SupportsIPv6 { get; set; }
    public List<string> AvailableSLAs { get; set; }
    public List<string> AvailableCoS { get; set; }
}

public class PricingIndicator
{
    public string PriceBand { get; set; }
    public decimal? EstimatedMRC { get; set; }
    public decimal? EstimatedNRC { get; set; }
    public string Currency { get; set; }
}

public class NetworkDetails
{
    public string PopLocation { get; set; }
    public double DistanceToPoP { get; set; }
    public string NetworkType { get; set; }
    public string LastMileProvider { get; set; }
}