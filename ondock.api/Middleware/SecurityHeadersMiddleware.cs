namespace ondock.api.Middleware;

/// <summary>
/// Adds security headers to all responses to protect against various web attacks
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: Prevents MIME type sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // X-Frame-Options: Prevents clickjacking attacks
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // X-XSS-Protection: Enables browser's XSS filter
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Strict-Transport-Security: Forces HTTPS
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        // Content-Security-Policy: Prevents XSS and data injection attacks
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://accounts.google.com https://appleid.cdn-apple.com; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' https://accounts.google.com https://appleid.apple.com; " +
            "frame-src https://accounts.google.com https://appleid.apple.com; " +
            "frame-ancestors 'none';");

        // Referrer-Policy: Controls how much referrer information is included with requests
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions-Policy: Restricts browser features
        context.Response.Headers.Append("Permissions-Policy",
            "geolocation=(self), " +
            "camera=(), " +
            "microphone=(), " +
            "payment=()");

        // Remove server header to prevent information disclosure
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await _next(context);
    }
}
