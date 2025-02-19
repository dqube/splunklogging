using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

public class SplunkHecExporter : BaseExporter<Activity>
{
    private readonly HttpClient _httpClient;
    private readonly string _hecEndpoint;
    private readonly string _hecToken;
    private readonly string _index;
    private readonly string _source;

    public SplunkHecExporter(string hecEndpoint, string hecToken, string index, string source)
    {
        _httpClient = new HttpClient();
        _hecEndpoint = hecEndpoint;
        _hecToken = hecToken;
        _index = index;
        _source = source;

        // Add the HEC token to the HTTP client headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Splunk {_hecToken}");
    }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        try
        {
            var events = new List<SplunkEvent>();

            foreach (var activity in batch)
            {
                // Convert OpenTelemetry Activity to Splunk event format
                var splunkEvent = new SplunkEvent
                {
                    Time = new DateTimeOffset(activity.StartTimeUtc).ToUnixTimeSeconds(),
                    Index = _index, // Set the Splunk index
                    Source = _source, // Set the Splunk source
                    Event = new Dictionary<string, object>
                    {
                        { "traceId", activity.TraceId.ToString() },
                        { "spanId", activity.SpanId.ToString() },
                        { "operationName", activity.OperationName },
                        { "startTime", activity.StartTimeUtc },
                        { "duration", activity.Duration.TotalMilliseconds },
                        { "tags", activity.Tags }
                    }
                };

                events.Add(splunkEvent);
            }

            // Send events to Splunk HEC
            var response = _httpClient.PostAsJsonAsync(
                _hecEndpoint,
                new { events },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            ).Result;

            response.EnsureSuccessStatusCode();

            return ExportResult.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error exporting to Splunk HEC: {ex.Message}");
            return ExportResult.Failure;
        }
    }

    protected override void Dispose(bool disposing)
    {
        _httpClient.Dispose();
        base.Dispose(disposing);
    }

    private class SplunkEvent
    {
        public double Time { get; set; } // Unix timestamp
        public string Index { get; set; } // Splunk index
        public string Source { get; set; } // Splunk source
        public Dictionary<string, object> Event { get; set; } // Event data
    }
}