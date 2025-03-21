using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Logs;

public class SplunkHecLogExporter : BaseExporter<LogRecord>
{
    private readonly HttpClient _httpClient;
    private readonly string _hecEndpoint;
    private readonly string _hecToken;
    private readonly string _index;
    private readonly string _source;

    public SplunkHecLogExporter(string hecEndpoint, string hecToken, string index, string source)
    {
        _httpClient = new HttpClient();
        _hecEndpoint = hecEndpoint;
        _hecToken = hecToken;
        _index = index;
        _source = source;

        // Add the HEC token to the HTTP client headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Splunk {_hecToken}");
    }

    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        try
        {
            var events = new List<SplunkEvent>();

            foreach (var log in batch)
            {
                var splunkEvent = new SplunkEvent
                {
                    Time = new DateTimeOffset(log.Timestamp).ToUnixTimeSeconds(),
                    Index = _index, // Set the Splunk index
                    Source = _source, // Set the Splunk source
                    Event = new Dictionary<string, object>
                    {
                        { "message", log.FormattedMessage ?? string.Empty },
                        { "severity", log.LogLevel },
                        { "category", log.CategoryName ?? string.Empty },
                        { "eventId", log.EventId.Id.ToString() },
                        { "eventName", log.EventId.Name ?? string.Empty },
                        { "traceId", log.TraceId.ToString() ?? string.Empty },
                        { "spanId", log.SpanId.ToString() ?? string.Empty },
                        { "method", log.Attributes?.FirstOrDefault(x => x.Key == "methodname").Value?.ToString() ?? string.Empty },
                        { "file", log.Attributes?.FirstOrDefault(x => x.Key == "filePath").Value?.ToString() ?? string.Empty },
                        { "className", log.Attributes?.FirstOrDefault(x => x.Key == "classname").Value?.ToString() ?? string.Empty },
                        { "expection", log.Exception?.Message ?? string.Empty }

                    }
                };

                events.Add(splunkEvent);
            }

            // var response = _httpClient.PostAsJsonAsync(_hecEndpoint, events).Result;

            // response.EnsureSuccessStatusCode();

            return ExportResult.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error exporting logs to Splunk HEC log exporter: {ex.Message}");
            return ExportResult.Failure;
        }
    }

    protected override void Dispose(bool disposing)
    {
        _httpClient.Dispose();
        base.Dispose(disposing);
    }
}
