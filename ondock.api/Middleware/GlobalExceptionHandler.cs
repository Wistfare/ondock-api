using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.Common;
using ondock.api.Exceptions;

namespace ondock.api.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandler(
        RequestDelegate next,
        ILogger<GlobalExceptionHandler> logger,
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
        // Log exception with full details internally (never exposed to client)
        LogException(exception, context);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetStatusCode(exception);

        var errorResponse = new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = GetErrorCode(exception),
                Message = GetSafeErrorMessage(exception),
                Details = GetSafeDetails(exception),
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path,
                TraceId = context.TraceIdentifier,
                ValidationErrors = GetValidationErrors(exception)
            }
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment(),
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(errorResponse, options);
        await context.Response.WriteAsync(json);
    }

    private void LogException(Exception exception, HttpContext context)
    {
        var logLevel = GetLogLevel(exception);
        var message = $"Exception occurred: {exception.GetType().Name} | Path: {context.Request.Path} | Method: {context.Request.Method}";

        switch (logLevel)
        {
            case LogLevel.Warning:
                _logger.LogWarning(exception, message);
                break;
            case LogLevel.Error:
                _logger.LogError(exception, message);
                break;
            case LogLevel.Critical:
                _logger.LogCritical(exception, message);
                break;
            default:
                _logger.LogInformation(exception, message);
                break;
        }
    }

    private static LogLevel GetLogLevel(Exception exception) => exception switch
    {
        BadRequestException => LogLevel.Warning,
        NotFoundException => LogLevel.Information,
        UnauthorizedException => LogLevel.Warning,
        ForbiddenException => LogLevel.Warning,
        ConflictException => LogLevel.Warning,
        NotImplementedException => LogLevel.Information,
        _ => LogLevel.Error
    };

    private static int GetStatusCode(Exception exception) => exception switch
    {
        BadRequestException => (int)HttpStatusCode.BadRequest,
        NotFoundException => (int)HttpStatusCode.NotFound,
        UnauthorizedException => (int)HttpStatusCode.Unauthorized,
        ForbiddenException => (int)HttpStatusCode.Forbidden,
        ConflictException => (int)HttpStatusCode.Conflict,
        NotImplementedException => (int)HttpStatusCode.NotImplemented,
        
        // Built-in exceptions (fallback only - prefer custom exceptions)
        ArgumentNullException => (int)HttpStatusCode.BadRequest,
        ArgumentException => (int)HttpStatusCode.BadRequest,
        KeyNotFoundException => (int)HttpStatusCode.NotFound,
        UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
        InvalidOperationException => (int)HttpStatusCode.Conflict,
        
        _ => (int)HttpStatusCode.InternalServerError
    };

    private static string GetErrorCode(Exception exception) => exception switch
    {
        BadRequestException => "BAD_REQUEST",
        NotFoundException => "NOT_FOUND",
        UnauthorizedException => "UNAUTHORIZED",
        ForbiddenException => "FORBIDDEN",
        ConflictException => "CONFLICT",
        NotImplementedException => "NOT_IMPLEMENTED",
        
        // Built-in exceptions
        ArgumentNullException => "INVALID_INPUT",
        ArgumentException => "INVALID_INPUT",
        KeyNotFoundException => "NOT_FOUND",
        UnauthorizedAccessException => "UNAUTHORIZED",
        InvalidOperationException => "CONFLICT",
        
        _ => "INTERNAL_SERVER_ERROR"
    };

    private string GetSafeErrorMessage(Exception exception)
    {
        // Only expose safe, user-friendly messages
        // Never expose technical details or stack traces in production
        
        if (exception is BadRequestException ||
            exception is NotFoundException ||
            exception is UnauthorizedException ||
            exception is ForbiddenException ||
            exception is ConflictException ||
            exception is NotImplementedException)
        {
            // Custom exceptions have safe messages designed for users
            return exception.Message;
        }

        // For all other exceptions, return generic message
        // Technical details are logged but never exposed to client
        return exception switch
        {
            ArgumentNullException => "Invalid or missing input data",
            ArgumentException => "Invalid input data provided",
            KeyNotFoundException => "The requested resource was not found",
            UnauthorizedAccessException => "Authentication is required to access this resource",
            InvalidOperationException => "The requested operation could not be completed",
            _ => "An unexpected error occurred. Please try again later or contact support if the problem persists."
        };
    }

    private string? GetSafeDetails(Exception exception)
    {
        // Only provide additional details in development environment
        // And only for exceptions that won't leak sensitive information
        if (!_environment.IsDevelopment())
        {
            return null;
        }

        // Even in development, never expose passwords, tokens, or connection strings
        return exception switch
        {
            BadRequestException or 
            NotFoundException or 
            UnauthorizedException or 
            ForbiddenException or 
            ConflictException => exception.Message,
            
            _ => SanitizeStackTrace(exception.StackTrace)
        };
    }

    private static List<ValidationError>? GetValidationErrors(Exception exception)
    {
        if (exception is BadRequestException badRequestEx && badRequestEx.ValidationErrors != null)
        {
            return badRequestEx.ValidationErrors
                .SelectMany(kvp => kvp.Value.Select(error => new ValidationError
                {
                    Field = kvp.Key,
                    Message = error
                }))
                .ToList();
        }

        return null;
    }

    private static string? SanitizeStackTrace(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
        {
            return null;
        }

        // Remove sensitive information from stack trace
        var sanitized = stackTrace;
        
        // Remove any potential connection strings
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, 
            @"(?i)(password|pwd|secret|token|key)\s*=\s*[^;\s]+", 
            "$1=***");

        return sanitized;
    }
}
