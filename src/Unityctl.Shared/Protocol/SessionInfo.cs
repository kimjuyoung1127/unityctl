using System.Text.Json.Serialization;

namespace Unityctl.Shared.Protocol;

/// <summary>
/// Session metadata DTO.
/// </summary>
public sealed class SessionInfo
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("projectPath")]
    public string ProjectPath { get; set; } = string.Empty;

    [JsonPropertyName("pipeName")]
    public string PipeName { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("unityVersion")]
    public string? UnityVersion { get; set; }

    [JsonPropertyName("unityPid")]
    public int? UnityPid { get; set; }
}
