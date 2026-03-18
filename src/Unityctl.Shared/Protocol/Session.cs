using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Unityctl.Shared.Protocol;

/// <summary>
/// Command execution session record.
/// Phase 3A: full session model stored in active.json / history.ndjson.
/// </summary>
public sealed class Session
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public SessionState State { get; set; }

    [JsonPropertyName("projectPath")]
    public string ProjectPath { get; set; } = string.Empty;

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("transport")]
    public string? Transport { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("pipeName")]
    public string? PipeName { get; set; }

    [JsonPropertyName("unityPid")]
    public int? UnityPid { get; set; }

    [JsonPropertyName("cliPid")]
    public int? CliPid { get; set; }

    [JsonPropertyName("result")]
    public JsonObject? Result { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }
}
