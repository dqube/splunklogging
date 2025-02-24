using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
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
        string requestMethod=string.Empty;
        string requestUri= context.Request.Path;
        string requestQueryString= context.Request.QueryString.ToString();
        // Capture the request
        var request = await FormatRequest(context.Request);

        // _logger.LogInformation("Request: {Request}", request);

        // Copy the original response body stream
        var originalBodyStream = context.Response.Body;

        using (var responseBody = new MemoryStream())
        {
            string responseStatusCode= context.Response.StatusCode.ToString();
            string responseTime = context.Response.Headers.ContainsKey("X-Response-Time") ? context.Response.Headers["X-Response-Time"].ToString() : string.Empty;
            string responseMessage = context.Response.Headers.ContainsKey("X-Response-Message") ? context.Response.Headers["X-Response-Message"].ToString() : string.Empty;
            string responseException = context.Response.Headers.ContainsKey("X-Response-Exception") ? context.Response.Headers["X-Response-Exception"].ToString() : string.Empty;
            string responseStackTrace = context.Response.Headers.ContainsKey("X-Response-StackTrace") ? context.Response.Headers["X-Response-StackTrace"].ToString() : string.Empty;
            string requestContent = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Response.Body = responseBody;

            await _next(context);

            // Capture the response
            var response = await FormatResponse(context.Response);

           // _logger.LogInformation("Response: {Response}", response);
            _logger.LogServiceCall("Service Call", requestMethod, requestUri, responseStatusCode, response, responseTime, responseMessage, responseException, responseStackTrace, requestQueryString, requestContent);

            // Copy the contents of the new memory stream to the original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task<string> FormatRequest(HttpRequest request)
    {
        request.EnableBuffering();

        var body = await new StreamReader(request.Body).ReadToEndAsync();
        request.Body.Position = 0;

        return $"Method: {request.Method}, Path: {request.Path}, QueryString: {request.QueryString}, Body: {body}";
    }

    private async Task<string> FormatResponse(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        return $"StatusCode: {response.StatusCode}, Body: {body}";
    }
}