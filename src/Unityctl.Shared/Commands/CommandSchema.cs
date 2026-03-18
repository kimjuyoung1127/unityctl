using System.Text.Json.Serialization;

namespace Unityctl.Shared.Commands;

/// <summary>
/// Top-level wrapper for the machine-readable schema output.
/// Includes version metadata alongside all command definitions.
/// </summary>
public sealed class CommandSchema
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("commands")]
    public CommandDefinition[] Commands { get; set; } = [];
}
