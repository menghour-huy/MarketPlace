using FluentValidation;
using Market.API.Common;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", new[] { exception.Message }),
            ValidationException validationException => (StatusCodes.Status400BadRequest, "Validation failed", validationException.Errors.Select(e => e.ErrorMessage).Distinct().ToArray()),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error", new[] { "An unexpected error occurred." })
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null,
            Errors = errors
        };

        await context.Response.WriteAsJsonAsync(payload);
    }
}
