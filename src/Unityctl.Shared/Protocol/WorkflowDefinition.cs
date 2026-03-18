using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Unityctl.Shared.Protocol;

/// <summary>
/// Defines a sequential workflow of unityctl commands to be executed.
/// </summary>
public sealed class WorkflowDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public WorkflowStep[] Steps { get; set; } = [];

    [JsonPropertyName("continueOnError")]
    public bool ContinueOnError { get; set; } = false;
}

/// <summary>
/// A single step in a workflow definition.
/// </summary>
public sealed class WorkflowStep
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("project")]
    public string? Project { get; set; }

    [JsonPropertyName("parameters")]
    public JsonObject? Parameters { get; set; }

    [JsonPropertyName("timeoutSeconds")]
    public int? TimeoutSeconds { get; set; }
}
