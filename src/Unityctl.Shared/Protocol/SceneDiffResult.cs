using System.Text.Json.Serialization;

namespace Unityctl.Shared.Protocol;

/// <summary>
/// The result of comparing two scene snapshots, organized by scene path.
/// </summary>
public sealed class SceneDiffResult
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>Timestamp or filename of the base (older) snapshot.</summary>
    [JsonPropertyName("baseSnapshot")]
    public string BaseSnapshot { get; set; } = string.Empty;

    /// <summary>Timestamp or filename of the head (newer) snapshot.</summary>
    [JsonPropertyName("headSnapshot")]
    public string HeadSnapshot { get; set; } = string.Empty;

    [JsonPropertyName("scenes")]
    public SceneDiffEntry[] Scenes { get; set; } = [];
}

/// <summary>
/// Diff results for a single scene.
/// </summary>
public sealed class SceneDiffEntry
{
    [JsonPropertyName("scenePath")]
    public string ScenePath { get; set; } = string.Empty;

    [JsonPropertyName("addedObjects")]
    public DiffObjectEntry[] AddedObjects { get; set; } = [];

    [JsonPropertyName("removedObjects")]
    public DiffObjectEntry[] RemovedObjects { get; set; } = [];

    [JsonPropertyName("modifiedObjects")]
    public ModifiedObjectEntry[] ModifiedObjects { get; set; } = [];
}

/// <summary>
/// A game object that was added or removed.
/// </summary>
public sealed class DiffObjectEntry
{
    [JsonPropertyName("globalObjectId")]
    public string GlobalObjectId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("scenePath")]
    public string ScenePath { get; set; } = string.Empty;
}

/// <summary>
/// A game object that was modified (components added, removed, or changed).
/// </summary>
public sealed class ModifiedObjectEntry
{
    [JsonPropertyName("globalObjectId")]
    public string GlobalObjectId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("addedComponents")]
    public ModifiedComponentEntry[] AddedComponents { get; set; } = [];

    [JsonPropertyName("removedComponents")]
    public ModifiedComponentEntry[] RemovedComponents { get; set; } = [];

    [JsonPropertyName("modifiedComponents")]
    public ComponentDiffEntry[] ModifiedComponents { get; set; } = [];
}

/// <summary>
/// A component that was added or removed from a game object.
/// </summary>
public sealed class ModifiedComponentEntry
{
    [JsonPropertyName("typeName")]
    public string TypeName { get; set; } = string.Empty;

    [JsonPropertyName("globalObjectId")]
    public string GlobalObjectId { get; set; } = string.Empty;
}

/// <summary>
/// A component that has property-level changes.
/// </summary>
public sealed class ComponentDiffEntry
{
    [JsonPropertyName("typeName")]
    public string TypeName { get; set; } = string.Empty;

    [JsonPropertyName("propertyChanges")]
    public PropertyChange[] PropertyChanges { get; set; } = [];
}

/// <summary>
/// A single property that changed between two snapshots.
/// </summary>
public sealed class PropertyChange
{
    [JsonPropertyName("propertyPath")]
    public string PropertyPath { get; set; } = string.Empty;

    /// <summary>String representation of the old value (display purposes).</summary>
    [JsonPropertyName("oldValue")]
    public string OldValue { get; set; } = string.Empty;

    /// <summary>String representation of the new value (display purposes).</summary>
    [JsonPropertyName("newValue")]
    public string NewValue { get; set; } = string.Empty;

    /// <summary>Type hint: "float", "int", "string", "bool", "object", etc.</summary>
    [JsonPropertyName("valueType")]
    public string ValueType { get; set; } = string.Empty;
}
