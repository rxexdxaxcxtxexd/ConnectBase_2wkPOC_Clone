using System.Net;
using System.Text.Json;
using ColteBondingAPI.Models.Responses;

namespace ColteBondingAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        context.Response.ContentType = "application/json";
        
        var response = new ErrorResponse
        {
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Code = "VALIDATION_ERROR";
                response.Message = validationEx.Message;
                response.Details = validationEx.Errors;
                break;

            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Code = "NOT_FOUND";
                response.Message = notFoundEx.Message;
                break;

            case UnauthorizedException unauthorizedEx:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Code = "UNAUTHORIZED";
                response.Message = unauthorizedEx.Message;
                break;

            case ForbiddenException forbiddenEx:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Code = "FORBIDDEN";
                response.Message = forbiddenEx.Message;
                break;

            case ConflictException conflictEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Code = "CONFLICT";
                response.Message = conflictEx.Message;
                break;

            case RateLimitExceededException rateLimitEx:
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                response.Code = "RATE_LIMIT_EXCEEDED";
                response.Message = rateLimitEx.Message;
                context.Response.Headers.Add("Retry-After", "60");
                break;

            case ExternalServiceException externalEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                response.Code = "EXTERNAL_SERVICE_ERROR";
                response.Message = "An error occurred while communicating with external service";
                if (_environment.IsDevelopment())
                {
                    response.Details = new { ExternalError = externalEx.Message };
                }
                break;

            case TimeoutException timeoutEx:
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.Code = "TIMEOUT";
                response.Message = "The request timed out";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Code = "INTERNAL_ERROR";
                response.Message = "An internal error occurred";
                
                if (_environment.IsDevelopment())
                {
                    response.Details = new
                    {
                        exception.Message,
                        exception.StackTrace
                    };
                }
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

// Custom Exception Classes
public class ValidationException : Exception
{
    public object Errors { get; set; }
    
    public ValidationException(string message, object errors = null) : base(message)
    {
        Errors = errors;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Unauthorized") : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Forbidden") : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message) : base(message) { }
    public ExternalServiceException(string message, Exception innerException) : base(message, innerException) { }
}