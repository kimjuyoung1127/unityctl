using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Unityctl.Shared.Protocol;

/// <summary>
/// Streaming event wrapper for IPC watch mode.
/// </summary>
public sealed class EventEnvelope
{
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("payload")]
    public JsonObject? Payload { get; set; }
}
