using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace ColteBondingAPI.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitSettings _settings;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<RateLimitSettings> settings)
    {
        _next = next;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        
        _logger.LogDebug("Rate limit check for client {ClientId}", clientId);

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get authenticated user ID
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            return context.User.Identity.Name ?? "authenticated";
        }

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        return !string.IsNullOrEmpty(ipAddress) ? ipAddress : "unknown";
    }
}

public class RateLimitSettings
{
    public int PermitLimit { get; set; } = 100;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
    public int QueueLimit { get; set; } = 50;
    public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;
}

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitSettings>(configuration.GetSection("RateLimiting"));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientId = GetClientIdentifier(context);
                
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientId,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 50
                    });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.Add("Retry-After", 
                        ((int)retryAfter.TotalSeconds).ToString());
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = "RATE_LIMIT_EXCEEDED",
                        message = "Too many requests. Please retry later.",
                        timestamp = DateTime.UtcNow
                    }
                });
            };
        });

        return services;
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            return context.User.Identity.Name ?? "authenticated";
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        return !string.IsNullOrEmpty(ipAddress) ? ipAddress : "unknown";
    }
}