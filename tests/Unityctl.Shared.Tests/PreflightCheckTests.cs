using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Shared.Tests;

public sealed class PreflightCheckTests
{
    // ─── Default values ───────────────────────────────────────────────────────

    [Fact]
    public void DefaultCategory_IsEmptyString()
    {
        var check = new PreflightCheck();
        Assert.Equal(string.Empty, check.Category);
    }

    [Fact]
    public void DefaultDetails_IsNull()
    {
        var check = new PreflightCheck();
        Assert.Null(check.Details);
    }

    // ─── Serialization round-trip ─────────────────────────────────────────────

    [Fact]
    public void Serializes_Roundtrip()
    {
        var original = new PreflightCheck
        {
            Category = "error",
            Check = "BuildTarget",
            Passed = false,
            Message = "Unknown build target: win32",
            Details = "Valid targets: StandaloneWindows64, ..."
        };

        var json = JsonSerializer.Serialize(original, UnityctlJsonContext.Default.PreflightCheck);
        var restored = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.PreflightCheck);

        Assert.NotNull(restored);
        Assert.Equal(original.Category, restored!.Category);
        Assert.Equal(original.Check, restored.Check);
        Assert.Equal(original.Passed, restored.Passed);
        Assert.Equal(original.Message, restored.Message);
        Assert.Equal(original.Details, restored.Details);
    }

    [Fact]
    public void Array_Serializes_Roundtrip()
    {
        var original = new PreflightCheck[]
        {
            new() { Category = "error", Check = "BuildTarget", Passed = false, Message = "Unknown target" },
            new() { Category = "warning", Check = "ActiveTargetMismatch", Passed = true, Message = "Active target differs" },
            new() { Category = "info", Check = "ScriptingBackend", Passed = true, Message = "IL2CPP" }
        };

        var json = JsonSerializer.Serialize(original, UnityctlJsonContext.Default.PreflightCheckArray);
        var restored = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.PreflightCheckArray);

        Assert.NotNull(restored);
        Assert.Equal(3, restored!.Length);
        Assert.Equal("error", restored[0].Category);
        Assert.Equal("warning", restored[1].Category);
        Assert.Equal("info", restored[2].Category);
        Assert.True(restored[2].Passed);
    }

    [Fact]
    public void NullDetails_OmittedInJson()
    {
        var check = new PreflightCheck
        {
            Category = "info",
            Check = "ScriptingBackend",
            Passed = true,
            Message = "Mono",
            Details = null
        };

        var json = JsonSerializer.Serialize(check, UnityctlJsonContext.Default.PreflightCheck);

        Assert.DoesNotContain("\"details\"", json);
    }

    [Fact]
    public void CamelCase_PropertyNames()
    {
        var check = new PreflightCheck
        {
            Category = "error",
            Check = "Compilation",
            Passed = false,
            Message = "Script errors"
        };

        var json = JsonSerializer.Serialize(check, UnityctlJsonContext.Default.PreflightCheck);

        Assert.Contains("\"category\"", json);
        Assert.Contains("\"check\"", json);
        Assert.Contains("\"passed\"", json);
        Assert.Contains("\"message\"", json);
    }

    [Fact]
    public void DetailsPresent_IncludedInJson()
    {
        var check = new PreflightCheck
        {
            Category = "info",
            Check = "DefineSymbols",
            Passed = true,
            Message = "DEBUG;DEVELOPMENT",
            Details = "from PlayerSettings"
        };

        var json = JsonSerializer.Serialize(check, UnityctlJsonContext.Default.PreflightCheck);

        Assert.Contains("\"details\"", json);
        Assert.Contains("from PlayerSettings", json);
    }

    // ─── Deserialization from wire format ─────────────────────────────────────

    [Fact]
    public void Deserialization_FromWireJson_Works()
    {
        var json = """
            {
              "category": "error",
              "check": "ScenesExist",
              "passed": false,
              "message": "No scenes enabled in Build Settings"
            }
            """;

        var check = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.PreflightCheck);

        Assert.NotNull(check);
        Assert.Equal("error", check!.Category);
        Assert.Equal("ScenesExist", check.Check);
        Assert.False(check.Passed);
        Assert.Null(check.Details);
    }
}
