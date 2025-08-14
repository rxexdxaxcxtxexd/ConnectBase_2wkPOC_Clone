using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ColteBondingAPI.Models.Requests;
using ColteBondingAPI.Models.Responses;
using ColteBondingAPI.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace ColteBondingAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;
    private readonly ILogger<AvailabilityController> _logger;

    public AvailabilityController(
        IAvailabilityService availabilityService,
        ILogger<AvailabilityController> logger)
    {
        _availabilityService = availabilityService;
        _logger = logger;
    }

    /// <summary>
    /// Check service availability for specified locations
    /// </summary>
    /// <param name="request">Availability check request</param>
    /// <returns>Service availability information</returns>
    /// <response code="200">Returns availability information</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("check")]
    [ProducesResponseType(typeof(AvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CheckAvailability(
        [FromBody][Required] AvailabilityCheckRequest request)
    {
        try
        {
            _logger.LogInformation("Checking availability for {LocationCount} locations", 
                request.Locations?.Count ?? 0);

            var result = await _availabilityService.CheckAvailabilityAsync(request);
            
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for availability check");
            return BadRequest(new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = ex.Message,
                Details = ex.Data
            });
        }
    }

    /// <summary>
    /// Get service availability by location ID
    /// </summary>
    /// <param name="locationId">Location identifier</param>
    /// <returns>Service availability for the location</returns>
    [HttpGet("{locationId}")]
    [ProducesResponseType(typeof(LocationAvailability), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocationAvailability(string locationId)
    {
        _logger.LogInformation("Getting availability for location {LocationId}", locationId);

        var result = await _availabilityService.GetLocationAvailabilityAsync(locationId);
        
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "LOCATION_NOT_FOUND",
                Message = $"Location {locationId} not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Batch check availability for multiple location pairs
    /// </summary>
    /// <param name="request">Batch availability request</param>
    /// <returns>Batch availability results</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(BatchAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchCheckAvailability(
        [FromBody][Required] BatchAvailabilityRequest request)
    {
        if (request.Requests?.Count > 10)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "BATCH_SIZE_EXCEEDED",
                Message = "Maximum batch size is 10 requests"
            });
        }

        _logger.LogInformation("Processing batch availability check for {RequestCount} requests", 
            request.Requests?.Count ?? 0);

        var result = await _availabilityService.BatchCheckAvailabilityAsync(request);
        
        return Ok(result);
    }
}