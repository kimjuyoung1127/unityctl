using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Unityctl.Shared.Protocol;

/// <summary>
/// A snapshot of all loaded scenes and their serialized object properties.
/// Captured by the plugin via SerializedObject API and returned as JSON.
/// </summary>
public sealed class SceneSnapshot
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("unityVersion")]
    public string UnityVersion { get; set; } = string.Empty;

    [JsonPropertyName("projectPath")]
    public string ProjectPath { get; set; } = string.Empty;

    [JsonPropertyName("sceneSetup")]
    public SceneSetupEntry[] SceneSetup { get; set; } = [];

    [JsonPropertyName("scenes")]
    public SceneEntry[] Scenes { get; set; } = [];
}

/// <summary>
/// Describes a scene in the editor's scene setup (loaded or unloaded).
/// </summary>
public sealed class SceneSetupEntry
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("isLoaded")]
    public bool IsLoaded { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

/// <summary>
/// A single loaded scene with its flat game object list.
/// </summary>
public sealed class SceneEntry
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("isDirty")]
    public bool IsDirty { get; set; }

    [JsonPropertyName("gameObjects")]
    public GameObjectEntry[] GameObjects { get; set; } = [];
}

/// <summary>
/// A game object entry in the snapshot (flat list — hierarchy expressed via ScenePath).
/// </summary>
public sealed class GameObjectEntry
{
    [JsonPropertyName("globalObjectId")]
    public string GlobalObjectId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("activeSelf")]
    public bool ActiveSelf { get; set; }

    [JsonPropertyName("layer")]
    public int Layer { get; set; }

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    /// <summary>Transform path from scene root, e.g. "Canvas/Panel/Button".</summary>
    [JsonPropertyName("scenePath")]
    public string ScenePath { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public ComponentEntry[] Components { get; set; } = [];
}

/// <summary>
/// A component on a game object, with its serialized properties.
/// </summary>
public sealed class ComponentEntry
{
    [JsonPropertyName("globalObjectId")]
    public string GlobalObjectId { get; set; } = string.Empty;

    [JsonPropertyName("typeName")]
    public string TypeName { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Property values keyed by propertyPath (e.g. "m_LocalPosition.x").
    /// Values are typed JSON nodes (numbers, strings, booleans, or nested objects).
    /// </summary>
    [JsonPropertyName("properties")]
    public JsonObject? Properties { get; set; }
}
