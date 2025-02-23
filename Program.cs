using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
// Register the SplunkHecExporter for tracing
// builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
// {
//     tracerProviderBuilder
//         .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyAspNetCoreApp"))
//         .AddAspNetCoreInstrumentation() // Instrument ASP.NET Core requests
//         .AddHttpClientInstrumentation() // Instrument HTTP requests
//         .AddProcessor(new BatchActivityExportProcessor(new SplunkHecExporter(
//             hecEndpoint: "https://your-splunk-hec-endpoint:8088/services/collector",
//             hecToken: "your-hec-token",
//             index: "your-splunk-index", // Specify the Splunk index
//             source: "your-splunk-source" // Specify the Splunk source
//         )));
// });

// Configure OpenTelemetry for Tracing
// builder.Services.AddOpenTelemetry()
//     .WithTracing(tracerProviderBuilder =>
//     {
//         tracerProviderBuilder
//             .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyAspNetCoreApp"))
//             .AddAspNetCoreInstrumentation() // Instrument ASP.NET Core requests
//             .AddHttpClientInstrumentation() // Instrument HTTP requests
//             .AddProcessor(new BatchActivityExportProcessor(new SplunkHecExporter(
//                 hecEndpoint: "https://your-splunk-hec-endpoint:8088/services/collector",
//                 hecToken: "your-hec-token",
//                 index: "your-splunk-index", // Specify the Splunk index
//                 source: "your-splunk-source" // Specify the Splunk source
//             )));
//     });
    
// Configure OpenTelemetry for Logging
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true; // Include formatted log messages
    options.IncludeScopes = true; // Include log scopes
    options.ParseStateValues = true; // Parse log state values
    options.AddProcessor(new BatchLogRecordExportProcessor(new SplunkHecLogExporter(
        hecEndpoint: "https://your-splunk-hec-endpoint:8088/services/collector",
        hecToken: "your-hec-token",
        index: "your-splunk-index", // Specify the Splunk index
        source: "your-splunk-source" // Specify the Splunk source
    )));
    // options.AddProcessor(new BatchLogRecordExportProcessor(new SQLLogExporter(
    //     connectionString: "your-sql-connection-string"
    // )));
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
{
    
    logger.LogInformation("Getting weather forecast");
     logger.LogStartExecutionTime("Starting data processing...");
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
        logger.LogExecutionTime("Finished data processing.");
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
