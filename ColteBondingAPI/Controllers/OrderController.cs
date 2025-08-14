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
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IOrderService orderService,
        ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new order
    /// </summary>
    /// <param name="request">Order submission request</param>
    /// <returns>Created order details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitOrder(
        [FromBody][Required] SubmitOrderRequest request)
    {
        _logger.LogInformation("Submitting order for quote {QuoteId}", request.QuoteId);

        var result = await _orderService.SubmitOrderAsync(request);
        
        return CreatedAtAction(
            nameof(GetOrder), 
            new { orderId = result.OrderId }, 
            result);
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <returns>Order details</returns>
    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(string orderId)
    {
        _logger.LogInformation("Retrieving order {OrderId}", orderId);

        var result = await _orderService.GetOrderAsync(orderId);
        
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "ORDER_NOT_FOUND",
                Message = $"Order {orderId} not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get order status
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <returns>Order status information</returns>
    [HttpGet("{orderId}/status")]
    [ProducesResponseType(typeof(OrderStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderStatus(string orderId)
    {
        _logger.LogInformation("Getting status for order {OrderId}", orderId);

        var result = await _orderService.GetOrderStatusAsync(orderId);
        
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "ORDER_NOT_FOUND",
                Message = $"Order {orderId} not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <param name="request">Cancellation request</param>
    /// <returns>Cancellation confirmation</returns>
    [HttpPost("{orderId}/cancel")]
    [ProducesResponseType(typeof(CancellationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(
        string orderId,
        [FromBody][Required] CancelOrderRequest request)
    {
        _logger.LogInformation("Cancelling order {OrderId}", orderId);

        var result = await _orderService.CancelOrderAsync(orderId, request);
        
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "ORDER_NOT_FOUND",
                Message = $"Order {orderId} not found"
            });
        }

        if (!result.Success)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "CANCELLATION_FAILED",
                Message = result.Reason
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Modify an existing order
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <param name="request">Modification request</param>
    /// <returns>Modified order details</returns>
    [HttpPut("{orderId}/modify")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ModifyOrder(
        string orderId,
        [FromBody][Required] ModifyOrderRequest request)
    {
        _logger.LogInformation("Modifying order {OrderId}", orderId);

        var result = await _orderService.ModifyOrderAsync(orderId, request);
        
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "ORDER_NOT_FOUND",
                Message = $"Order {orderId} not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// List orders with filtering and pagination
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>Paginated list of orders</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOrders([FromQuery] OrderFilterRequest filter)
    {
        _logger.LogInformation("Listing orders with filter");

        var result = await _orderService.ListOrdersAsync(filter);
        
        return Ok(result);
    }

    /// <summary>
    /// Get order milestones
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <returns>Order milestones</returns>
    [HttpGet("{orderId}/milestones")]
    [ProducesResponseType(typeof(MilestonesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderMilestones(string orderId)
    {
        _logger.LogInformation("Getting milestones for order {OrderId}", orderId);

        var result = await _orderService.GetOrderMilestonesAsync(orderId);
        
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "ORDER_NOT_FOUND",
                Message = $"Order {orderId} not found"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Add note to an order
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <param name="request">Note request</param>
    /// <returns>Note confirmation</returns>
    [HttpPost("{orderId}/notes")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddOrderNote(
        string orderId,
        [FromBody][Required] AddNoteRequest request)
    {
        _logger.LogInformation("Adding note to order {OrderId}", orderId);

        var result = await _orderService.AddOrderNoteAsync(orderId, request);
        
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "ORDER_NOT_FOUND",
                Message = $"Order {orderId} not found"
            });
        }

        return CreatedAtAction(
            nameof(GetOrder), 
            new { orderId = orderId }, 
            result);
    }
}