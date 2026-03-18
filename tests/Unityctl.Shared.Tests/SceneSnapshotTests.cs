using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Shared.Tests;

public class SceneSnapshotTests
{
    [Fact]
    public void SceneSnapshot_RoundTrip_PreservesAllFields()
    {
        var snapshot = new SceneSnapshot
        {
            Timestamp = "2026-03-18T12:00:00.000Z",
            UnityVersion = "2022.3.20f1",
            ProjectPath = "/Users/dev/MyProject",
            SceneSetup =
            [
                new SceneSetupEntry { Path = "Assets/Scenes/Main.unity", IsLoaded = true, IsActive = true }
            ],
            Scenes =
            [
                new SceneEntry
                {
                    Path = "Assets/Scenes/Main.unity",
                    Name = "Main",
                    IsDirty = false,
                    GameObjects =
                    [
                        new GameObjectEntry
                        {
                            GlobalObjectId = "GlobalObjectId_V1-2-abc123-456-0",
                            Name = "Player",
                            ActiveSelf = true,
                            Layer = 0,
                            Tag = "Player",
                            ScenePath = "Player",
                            Components =
                            [
                                new ComponentEntry
                                {
                                    GlobalObjectId = "GlobalObjectId_V1-3-abc123-789-0",
                                    TypeName = "UnityEngine.Transform",
                                    Enabled = true,
                                    Properties = new JsonObject
                                    {
                                        ["m_LocalPosition.x"] = 1.5,
                                        ["m_LocalPosition.y"] = 0.0
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var json = JsonSerializer.Serialize(snapshot, UnityctlJsonContext.Default.SceneSnapshot);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.SceneSnapshot);

        Assert.NotNull(deserialized);
        Assert.Equal("2026-03-18T12:00:00.000Z", deserialized!.Timestamp);
        Assert.Equal("2022.3.20f1", deserialized.UnityVersion);
        Assert.Equal("/Users/dev/MyProject", deserialized.ProjectPath);
        Assert.Single(deserialized.SceneSetup);
        Assert.Equal("Assets/Scenes/Main.unity", deserialized.SceneSetup[0].Path);
        Assert.True(deserialized.SceneSetup[0].IsLoaded);
        Assert.True(deserialized.SceneSetup[0].IsActive);
        Assert.Single(deserialized.Scenes);

        var scene = deserialized.Scenes[0];
        Assert.Equal("Main", scene.Name);
        Assert.False(scene.IsDirty);
        Assert.Single(scene.GameObjects);

        var go = scene.GameObjects[0];
        Assert.Equal("Player", go.Name);
        Assert.Equal("Player", go.Tag);
        Assert.Equal("Player", go.ScenePath);
        Assert.True(go.ActiveSelf);
        Assert.Single(go.Components);

        var comp = go.Components[0];
        Assert.Equal("UnityEngine.Transform", comp.TypeName);
        Assert.True(comp.Enabled);
        Assert.NotNull(comp.Properties);
        var posX = comp.Properties!["m_LocalPosition.x"]?.GetValue<double>();
        Assert.NotNull(posX);
        Assert.True(Math.Abs(posX!.Value - 1.5) < 1e-6);
    }

    [Fact]
    public void SceneSnapshot_EmptyScenes_Serializes()
    {
        var snapshot = new SceneSnapshot
        {
            Timestamp = "2026-03-18T12:00:00.000Z",
            UnityVersion = "2022.3.20f1",
            ProjectPath = "/project",
            SceneSetup = [],
            Scenes = []
        };

        var json = JsonSerializer.Serialize(snapshot, UnityctlJsonContext.Default.SceneSnapshot);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.SceneSnapshot);

        Assert.NotNull(deserialized);
        Assert.Empty(deserialized!.SceneSetup);
        Assert.Empty(deserialized.Scenes);
    }

    [Fact]
    public void SceneSnapshot_NullProperties_OmittedInJson()
    {
        var comp = new ComponentEntry
        {
            GlobalObjectId = "id1",
            TypeName = "MyScript",
            Enabled = true,
            Properties = null
        };

        var json = JsonSerializer.Serialize(comp, UnityctlJsonContext.Default.ComponentEntry);

        Assert.DoesNotContain("\"properties\"", json);
    }

    [Fact]
    public void SceneDiffResult_RoundTrip_PreservesAllFields()
    {
        var result = new SceneDiffResult
        {
            Timestamp = "2026-03-18T12:01:00.000Z",
            BaseSnapshot = "2026-03-18T12:00:00.000Z",
            HeadSnapshot = "2026-03-18T12:01:00.000Z",
            Scenes =
            [
                new SceneDiffEntry
                {
                    ScenePath = "Assets/Scenes/Main.unity",
                    AddedObjects = [],
                    RemovedObjects = [],
                    ModifiedObjects =
                    [
                        new ModifiedObjectEntry
                        {
                            GlobalObjectId = "id1",
                            Name = "Player",
                            AddedComponents = [],
                            RemovedComponents = [],
                            ModifiedComponents =
                            [
                                new ComponentDiffEntry
                                {
                                    TypeName = "UnityEngine.Transform",
                                    PropertyChanges =
                                    [
                                        new PropertyChange
                                        {
                                            PropertyPath = "m_LocalPosition.x",
                                            OldValue = "1.5",
                                            NewValue = "2.0",
                                            ValueType = "float"
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var json = JsonSerializer.Serialize(result, UnityctlJsonContext.Default.SceneDiffResult);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.SceneDiffResult);

        Assert.NotNull(deserialized);
        Assert.Equal("2026-03-18T12:01:00.000Z", deserialized!.Timestamp);
        Assert.Equal("2026-03-18T12:00:00.000Z", deserialized.BaseSnapshot);
        Assert.Single(deserialized.Scenes);

        var scene = deserialized.Scenes[0];
        Assert.Equal("Assets/Scenes/Main.unity", scene.ScenePath);
        Assert.Single(scene.ModifiedObjects);

        var obj = scene.ModifiedObjects[0];
        Assert.Equal("Player", obj.Name);
        Assert.Single(obj.ModifiedComponents);

        var comp = obj.ModifiedComponents[0];
        Assert.Equal("UnityEngine.Transform", comp.TypeName);
        Assert.Single(comp.PropertyChanges);

        var change = comp.PropertyChanges[0];
        Assert.Equal("m_LocalPosition.x", change.PropertyPath);
        Assert.Equal("1.5", change.OldValue);
        Assert.Equal("2.0", change.NewValue);
        Assert.Equal("float", change.ValueType);
    }

    [Fact]
    public void SceneDiffResult_EmptyChanges_Serializes()
    {
        var result = new SceneDiffResult
        {
            Timestamp = "2026-03-18T12:01:00.000Z",
            BaseSnapshot = "snap1",
            HeadSnapshot = "snap2",
            Scenes = []
        };

        var json = JsonSerializer.Serialize(result, UnityctlJsonContext.Default.SceneDiffResult);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.SceneDiffResult);

        Assert.NotNull(deserialized);
        Assert.Empty(deserialized!.Scenes);
    }

    [Fact]
    public void PropertyChange_FloatValues_PreservedAsString()
    {
        var change = new PropertyChange
        {
            PropertyPath = "m_LocalScale.x",
            OldValue = "1.5",
            NewValue = "2.0000001",
            ValueType = "float"
        };

        var json = JsonSerializer.Serialize(change, UnityctlJsonContext.Default.PropertyChange);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.PropertyChange);

        Assert.NotNull(deserialized);
        Assert.Equal("1.5", deserialized!.OldValue);
        Assert.Equal("2.0000001", deserialized.NewValue);
        Assert.Equal("float", deserialized.ValueType);
    }

    [Fact]
    public void SceneSetupEntry_RoundTrip()
    {
        var entry = new SceneSetupEntry
        {
            Path = "Assets/Levels/Level1.unity",
            IsLoaded = false,
            IsActive = false
        };

        var json = JsonSerializer.Serialize(entry, UnityctlJsonContext.Default.SceneSetupEntry);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.SceneSetupEntry);

        Assert.NotNull(deserialized);
        Assert.Equal("Assets/Levels/Level1.unity", deserialized!.Path);
        Assert.False(deserialized.IsLoaded);
        Assert.False(deserialized.IsActive);
    }

    [Fact]
    public void GameObjectEntry_WithComponents_RoundTrip()
    {
        var go = new GameObjectEntry
        {
            GlobalObjectId = "goid-001",
            Name = "Enemy",
            ActiveSelf = false,
            Layer = 8,
            Tag = "Enemy",
            ScenePath = "Enemies/Enemy",
            Components =
            [
                new ComponentEntry { TypeName = "EnemyAI", Enabled = true },
                new ComponentEntry { TypeName = "UnityEngine.BoxCollider", Enabled = false }
            ]
        };

        var json = JsonSerializer.Serialize(go, UnityctlJsonContext.Default.GameObjectEntry);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.GameObjectEntry);

        Assert.NotNull(deserialized);
        Assert.Equal("Enemy", deserialized!.Name);
        Assert.Equal(8, deserialized.Layer);
        Assert.False(deserialized.ActiveSelf);
        Assert.Equal("Enemies/Enemy", deserialized.ScenePath);
        Assert.Equal(2, deserialized.Components.Length);
        Assert.Equal("EnemyAI", deserialized.Components[0].TypeName);
        Assert.False(deserialized.Components[1].Enabled);
    }

    [Fact]
    public void ComponentEntry_WithJsonObjectProperties_RoundTrip()
    {
        var comp = new ComponentEntry
        {
            GlobalObjectId = "comp-001",
            TypeName = "UnityEngine.Transform",
            Enabled = true,
            Properties = new JsonObject
            {
                ["m_LocalPosition.x"] = 3.14,
                ["m_LocalPosition.y"] = -1.0,
                ["m_LocalScale.x"] = 1.0,
                ["m_Name"] = "Player"
            }
        };

        var json = JsonSerializer.Serialize(comp, UnityctlJsonContext.Default.ComponentEntry);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.ComponentEntry);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized!.Properties);
        Assert.Equal(4, deserialized.Properties!.Count);
        Assert.True(Math.Abs(deserialized.Properties["m_LocalPosition.x"]!.GetValue<double>() - 3.14) < 1e-6);
        Assert.True(Math.Abs(deserialized.Properties["m_LocalPosition.y"]!.GetValue<double>() - (-1.0)) < 1e-6);
        Assert.Equal("Player", deserialized.Properties["m_Name"]!.GetValue<string>());
    }
}
