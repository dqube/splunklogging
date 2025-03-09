using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            // Log the exception
            _logger.LogError(ex, "An unhandled exception occurred.");

            // Add the exception details to the response headers
            context.Response.Headers["X-Response-Exception"] = ex.Message;
            context.Response.Headers["X-Response-StackTrace"] = ex.StackTrace;

            // Optionally, set a custom status code for the response
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // Optionally, write a custom error response
            await context.Response.WriteAsync("An error occurred. Please try again later.");
        }
    }
}