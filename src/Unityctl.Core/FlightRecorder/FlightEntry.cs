using System.Text.Json.Serialization;

namespace Unityctl.Core.FlightRecorder;

/// <summary>
/// A single entry in the flight recorder log.
/// Phase 3B: full implementation.
/// </summary>
public sealed class FlightEntry
{
    [JsonPropertyName("ts")]
    public long Timestamp { get; set; }

    [JsonPropertyName("op")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("project")]
    public string? Project { get; set; }

    [JsonPropertyName("transport")]
    public string? Transport { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("durationMs")]
    public long DurationMs { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }
}
