using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Unityctl.Shared.Protocol;

namespace Unityctl.Core.Sessions;

/// <summary>
/// Compact (non-indented) JSON serialization context for session NDJSON files.
/// Separate from UnityctlJsonContext to avoid WriteIndented=true breaking NDJSON format.
/// </summary>
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(Session[]))]
[JsonSerializable(typeof(SessionState))]
[JsonSerializable(typeof(JsonObject))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class SessionJsonContext : JsonSerializerContext
{
}
