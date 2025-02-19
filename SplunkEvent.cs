internal class SplunkEvent
{
    public double Time { get; set; } // Unix timestamp
    public string? Index { get; set; } // Splunk index
    public string? Source { get; set; } // Splunk source
    public Dictionary<string, object>? Event { get; set; } // Event data
}