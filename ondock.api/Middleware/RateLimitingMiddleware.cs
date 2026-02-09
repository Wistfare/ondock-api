using System.Collections.Concurrent;
using System.Net;

namespace ondock.api.Middleware;

/// <summary>
/// Simple rate limiting middleware to prevent brute force attacks
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    
    // Store request counts: IP -> (LastReset, RequestCount)
    private static readonly ConcurrentDictionary<string, (DateTime LastReset, int Count)> _requestCounts = new();
    
    // Configuration - increased for development with multiple polling streams
    // Map polling (5s interval), status polling, chat polling, announcements = many requests
    private const int MaxRequestsPerMinute = 300; // General limit (was 60, too low for polling)
    private const int MaxAuthRequestsPerMinute = 20; // Strict limit for auth endpoints
    private const int WindowSizeSeconds = 60;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = GetClientIpAddress(context);
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Determine rate limit based on endpoint
        var isAuthEndpoint = path.Contains("/api/v1/auth/login") || 
                            path.Contains("/api/v1/auth/register") ||
                            path.Contains("/api/v1/auth/forgot-password");
        
        var maxRequests = isAuthEndpoint ? MaxAuthRequestsPerMinute : MaxRequestsPerMinute;

        if (!IsRequestAllowed(ipAddress, maxRequests, isAuthEndpoint))
        {
            _logger.LogWarning("Rate limit exceeded for IP: {IpAddress} on path: {Path}", ipAddress, path);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    message = "Too many requests. Please try again later.",
                    statusCode = 429
                }
            });
            return;
        }

        await _next(context);
    }

    private bool IsRequestAllowed(string ipAddress, int maxRequests, bool isAuthEndpoint)
    {
        var now = DateTime.UtcNow;
        
        var (lastReset, count) = _requestCounts.GetOrAdd(ipAddress, _ => (now, 0));
        
        // Reset counter if window has passed
        if ((now - lastReset).TotalSeconds > WindowSizeSeconds)
        {
            _requestCounts[ipAddress] = (now, 1);
            return true;
        }
        
        // Check if limit exceeded
        if (count >= maxRequests)
        {
            return false;
        }
        
        // Increment counter
        _requestCounts[ipAddress] = (lastReset, count + 1);
        return true;
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Try to get IP from X-Forwarded-For header (if behind proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',');
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Fallback to RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
