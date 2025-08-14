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
public class QuoteController : ControllerBase
{
    private readonly IQuoteService _quoteService;
    private readonly ILogger<QuoteController> _logger;

    public QuoteController(
        IQuoteService quoteService,
        ILogger<QuoteController> logger)
    {
        _quoteService = quoteService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new quote
    /// </summary>
    /// <param name="request">Quote creation request</param>
    /// <returns>Created quote details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateQuote(
        [FromBody][Required] CreateQuoteRequest request)
    {
        _logger.LogInformation("Creating quote for customer {CustomerId}", 
            request.Customer?.AccountNumber);

        var result = await _quoteService.CreateQuoteAsync(request);
        
        return CreatedAtAction(
            nameof(GetQuote), 
            new { quoteId = result.QuoteId }, 
            result);
    }

    /// <summary>
    /// Get quote by ID
    /// </summary>
    /// <param name="quoteId">Quote identifier</param>
    /// <returns>Quote details</returns>
    [HttpGet("{quoteId}")]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuote(string quoteId)
    {
        _logger.LogInformation("Retrieving quote {QuoteId}", quoteId);

        var result = await _quoteService.GetQuoteAsync(quoteId);
        
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "QUOTE_NOT_FOUND",
                Message = $"Quote {quoteId} not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Update an existing quote
    /// </summary>
    /// <param name="quoteId">Quote identifier</param>
    /// <param name="request">Quote update request</param>
    /// <returns>Updated quote details</returns>
    [HttpPut("{quoteId}")]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateQuote(
        string quoteId,
        [FromBody][Required] UpdateQuoteRequest request)
    {
        _logger.LogInformation("Updating quote {QuoteId}", quoteId);

        var result = await _quoteService.UpdateQuoteAsync(quoteId, request);
        
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "QUOTE_NOT_FOUND",
                Message = $"Quote {quoteId} not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a quote
    /// </summary>
    /// <param name="quoteId">Quote identifier</param>
    /// <returns>No content</returns>
    [HttpDelete("{quoteId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuote(string quoteId)
    {
        _logger.LogInformation("Deleting quote {QuoteId}", quoteId);

        var deleted = await _quoteService.DeleteQuoteAsync(quoteId);
        
        if (!deleted)
        {
            return NotFound(new ErrorResponse
            {
                Code = "QUOTE_NOT_FOUND",
                Message = $"Quote {quoteId} not found"
            });
        }

        return NoContent();
    }

    /// <summary>
    /// List quotes with filtering and pagination
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>Paginated list of quotes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<QuoteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListQuotes([FromQuery] QuoteFilterRequest filter)
    {
        _logger.LogInformation("Listing quotes with filter");

        var result = await _quoteService.ListQuotesAsync(filter);
        
        return Ok(result);
    }

    /// <summary>
    /// Validate a quote
    /// </summary>
    /// <param name="quoteId">Quote identifier</param>
    /// <returns>Validation result</returns>
    [HttpPost("{quoteId}/validate")]
    [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateQuote(string quoteId)
    {
        _logger.LogInformation("Validating quote {QuoteId}", quoteId);

        var result = await _quoteService.ValidateQuoteAsync(quoteId);
        
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "QUOTE_NOT_FOUND",
                Message = $"Quote {quoteId} not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Clone an existing quote
    /// </summary>
    /// <param name="quoteId">Source quote identifier</param>
    /// <returns>Cloned quote details</returns>
    [HttpPost("{quoteId}/clone")]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloneQuote(string quoteId)
    {
        _logger.LogInformation("Cloning quote {QuoteId}", quoteId);

        var result = await _quoteService.CloneQuoteAsync(quoteId);
        
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "QUOTE_NOT_FOUND",
                Message = $"Quote {quoteId} not found"
            });
        }

        return CreatedAtAction(
            nameof(GetQuote), 
            new { quoteId = result.QuoteId }, 
            result);
    }
}