using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the request has a correlation ID in the headers or request properties
        if (!context.Request.Headers.ContainsKey("X-Correlation-ID") &&
            !context.Items.ContainsKey("X-Correlation-ID"))
        {
            // Generate a new correlation ID (e.g., a GUID)
            var correlationId = Guid.NewGuid().ToString();

            // Add the correlation ID to the request headers
            context.Request.Headers.Add("X-Correlation-ID", correlationId);

            // Add the correlation ID to the request properties (HttpContext.Items)
            context.Items["X-Correlation-ID"] = correlationId;
        }

        // Capture the correlation ID from headers or request properties
        var correlationIdValue = context.Request.Headers["X-Correlation-ID"].ToString() ??
                                 context.Items["X-Correlation-ID"]?.ToString();

        // Capture request details
        string requestMethod = context.Request.Method;
        string requestUri = context.Request.Path;
        string requestQueryString = context.Request.QueryString.ToString();
        string requestContent = await FormatRequest(context.Request);

        // Copy the original response body stream
        var originalBodyStream = context.Response.Body;

        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            // Start the stopwatch to measure the response time
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Call the next middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log the exception if something goes wrong
                _logger.LogError(ex, "An error occurred while processing the request.");
                throw; // Re-throw the exception to ensure the error is handled by other middleware
            }
            finally
            {
                // Stop the stopwatch and get the elapsed time
                stopwatch.Stop();
                string responseTime = stopwatch.ElapsedMilliseconds.ToString();

                // Capture the response
                var response = await FormatResponse(context.Response);

                string responseStatusCode = context.Response.StatusCode.ToString();
                string responseMessage = context.Response.Headers.ContainsKey("X-Response-Message") ? context.Response.Headers["X-Response-Message"].ToString() : string.Empty;
                string responseException = context.Response.Headers.ContainsKey("X-Response-Exception") ? context.Response.Headers["X-Response-Exception"].ToString() : string.Empty;
                string responseStackTrace = context.Response.Headers.ContainsKey("X-Response-StackTrace") ? context.Response.Headers["X-Response-StackTrace"].ToString() : string.Empty;

                // Log the request and response details with the correlation ID
                _logger.LogInformation(
                    "Correlation ID: {CorrelationId}\n" +
                    "Request: {Method} {Uri} {QueryString} {RequestBody}\n" +
                    "Response: {StatusCode} {ResponseBody} {ResponseTime}ms\n" +
                    "Message: {ResponseMessage}\n" +
                    "Exception: {ResponseException}\n" +
                    "StackTrace: {ResponseStackTrace}",
                    correlationIdValue,
                    requestMethod, requestUri, requestQueryString, requestContent,
                    responseStatusCode, response, responseTime,
                    responseMessage, responseException, responseStackTrace);

                // Copy the contents of the new memory stream to the original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
        }
    }

    private async Task<string> FormatRequest(HttpRequest request)
    {
        request.EnableBuffering();

        var body = await new StreamReader(request.Body).ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }

    private async Task<string> FormatResponse(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        return body;
    }
}