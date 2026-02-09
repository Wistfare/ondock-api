using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Extensions;
using ondock.api.Hubs;
using ondock.api.Services.Interfaces;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Prometheus;
using Serilog;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to support HTTP/2 without TLS for gRPC in development
// Note: gRPC requires HTTP/2, and h2c (HTTP/2 cleartext) needs explicit config
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel((context, options) =>
    {
        options.ListenAnyIP(5028, listenOptions =>
        {
            // Http2 only for gRPC - this enables h2c (HTTP/2 prior knowledge)
            listenOptions.Protocols = HttpProtocols.Http2;
        });
        // Separate HTTP/1.1 port for REST API and Swagger
        options.ListenAnyIP(5029, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1;
        });
    });
    
    // Clear the URLs from launchSettings to use only Kestrel config
    builder.WebHost.UseUrls();
}

// Configure Serilog with full exception details
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
        .WithDefaultDestructurers()
        .WithDestructurers(new[] { new DbUpdateExceptionDestructurer() })));

// Add services to the container using extension methods
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationSettings(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddGrpcServices(); // Add gRPC services
builder.Services.AddMapsterConfiguration();
builder.Services.AddCorsPolicy();
builder.Services.AddHangfireServices(builder.Configuration);
builder.Services.AddApiControllers();
builder.Services.AddSwaggerDocumentation();

// Add health checks
builder.Services.AddHealthChecks();

// Add OpenTelemetry metrics for full stack tracing
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "ondock-api", serviceVersion: "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Microsoft.AspNetCore.Hosting")
        .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
        .AddMeter("System.Net.Http")
        .AddPrometheusExporter());

var app = builder.Build();

if (app.Configuration.GetValue<bool>("ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OnDockDbContext>();
    dbContext.Database.Migrate();

    if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("CleanupDatabase"))
    {
        DatabaseSeeder.CleanupDatabase(dbContext);
        DatabaseSeeder.ForceSeedProfileData(dbContext);
    }
    else if (app.Configuration.GetValue<bool>("CleanupDatabase"))
    {
        Console.WriteLine("WARNING: CleanupDatabase is enabled but ignored - only allowed in Development environment.");
    }

    // Traditional seeding - ensures seed data exists (runs in ALL environments)
    DatabaseSeeder.SeedProfileData(dbContext);
}

// Configure the HTTP request pipeline.

// Serilog request logging - logs all HTTP requests with timing and status
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ??
                                            httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        }
    };
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

// Security headers - add first to ensure all responses have security headers
app.UseMiddleware<ondock.api.Middleware.SecurityHeadersMiddleware>();

// Rate limiting - protect against brute force attacks
app.UseMiddleware<ondock.api.Middleware.RateLimitingMiddleware>();

// Global exception handler
app.UseMiddleware<ondock.api.Middleware.GlobalExceptionHandler>();

// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI();
// }

//app.UseHttpsRedirection();

// Enable static files for wwwroot content (HTML pages, etc.)
app.UseStaticFiles();

// Use CORS - choose policy based on environment
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "Production");

// Use Hangfire Dashboard (only in development for security)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
        {
            RequireSsl = false,
            SslRedirect = false,
            LoginCaseSensitive = true,
            Users = new []
            {
                new BasicAuthAuthorizationUser
                {
                    Login = "admin",
                    PasswordClear = "admin123" // Change this in production!
                }
            }
        })}
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { status = "healthy", service = "OnDock API", timestamp = DateTime.UtcNow }));

// Prometheus metrics endpoints
app.UseHttpMetrics();  // Collect HTTP request metrics
app.MapMetrics("/metrics");  // Expose metrics at /metrics endpoint
app.UseOpenTelemetryPrometheusScrapingEndpoint("/otel-metrics");  // OpenTelemetry metrics

app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<LoadViewHub>("/hubs/loadview");

// Map gRPC services
app.MapGrpcServices();

RecurringJob.AddOrUpdate<IChatRoomScanner>(
    "chat-room-scan",
    s => s.ScanAsync(),
    Cron.Minutely);

app.Run();