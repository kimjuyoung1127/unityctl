using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Unityctl.Shared.Protocol;

public sealed class CommandRequest
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public JsonObject? Parameters { get; set; }

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Get a string parameter. Returns defaultValue if key is missing or null.
    /// </summary>
    public string? GetParam(string key, string? defaultValue = null)
        => Parameters?.TryGetPropertyValue(key, out var node) == true
            ? node?.GetValue<string>() : defaultValue;

    /// <summary>
    /// Get a typed parameter. Supports int, bool, long, double, string.
    /// Returns defaultValue if key is missing, null, or wrong type.
    /// </summary>
    public T GetParam<T>(string key, T defaultValue = default!) where T : struct
    {
        if (Parameters?.TryGetPropertyValue(key, out var node) != true || node == null)
            return defaultValue;

        try
        {
            return node.GetValue<T>();
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Get a nested JsonObject parameter.
    /// Returns null if key is missing or not an object.
    /// </summary>
    public JsonObject? GetObjectParam(string key)
    {
        if (Parameters?.TryGetPropertyValue(key, out var node) != true)
            return null;
        return node as JsonObject;
    }
}
