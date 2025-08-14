using System.ComponentModel.DataAnnotations;

namespace ColteBondingAPI.Models.Requests;

public class AvailabilityCheckRequest
{
    [Required]
    [MinLength(1)]
    public List<Location> Locations { get; set; }

    [Required]
    public string ServiceType { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Bandwidth { get; set; }

    public string BandwidthUnit { get; set; } = "Mbps";
}

public class BatchAvailabilityRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(10)]
    public List<AvailabilityCheckRequest> Requests { get; set; }
}

public class Location
{
    [Required]
    public LocationType Type { get; set; }

    public Address Address { get; set; }
    public Coordinates Coordinates { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == LocationType.Address && Address == null)
        {
            yield return new ValidationResult("Address is required when Type is Address");
        }

        if (Type == LocationType.Coordinates && Coordinates == null)
        {
            yield return new ValidationResult("Coordinates are required when Type is Coordinates");
        }
    }
}

public class Address
{
    [Required]
    public string StreetAddress { get; set; }
    
    public string StreetAddress2 { get; set; }
    
    [Required]
    public string City { get; set; }
    
    public string State { get; set; }
    
    [Required]
    public string PostalCode { get; set; }
    
    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string Country { get; set; }
}

public class Coordinates
{
    [Required]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public double Longitude { get; set; }

    public double? Accuracy { get; set; }
}

public enum LocationType
{
    Address,
    Coordinates
}