using System.Text.Json;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Shared.Tests;

/// <summary>
/// Tests for FlightEntry model — default values, serialization round-trip,
/// and JSON field naming conventions.
/// </summary>
public sealed class FlightEntryTests
{
    // ─── Default values ───────────────────────────────────────────────────────

    [Fact]
    public void DefaultOperation_IsEmptyString()
    {
        var entry = new FlightEntry();
        Assert.Equal(string.Empty, entry.Operation);
    }

    [Fact]
    public void DefaultLevel_IsInfo()
    {
        var entry = new FlightEntry();
        Assert.Equal("info", entry.Level);
    }

    [Fact]
    public void DefaultV_IsEmptyString()
    {
        var entry = new FlightEntry();
        Assert.Equal(string.Empty, entry.V);
    }

    [Fact]
    public void NullableFields_AreNullByDefault()
    {
        var entry = new FlightEntry();

        Assert.Null(entry.Project);
        Assert.Null(entry.Transport);
        Assert.Null(entry.RequestId);
        Assert.Null(entry.ExitCode);
        Assert.Null(entry.Error);
        Assert.Null(entry.UnityVersion);
        Assert.Null(entry.Machine);
        Assert.Null(entry.Args);
        Assert.Null(entry.Sid);
    }

    // ─── JSON property names ──────────────────────────────────────────────────

    [Fact]
    public void Serialization_UsesShortPropertyNames()
    {
        var entry = new FlightEntry
        {
            Timestamp = 1742313825000L,
            Operation = "build",
            Level = "info",
            V = "0.2.0"
        };

        var json = JsonSerializer.Serialize(entry);

        Assert.Contains("\"ts\"", json);
        Assert.Contains("\"op\"", json);
        Assert.Contains("\"level\"", json);
        Assert.Contains("\"v\"", json);
    }

    [Fact]
    public void Serialization_NullFieldsAreOmitted_WithWhenWritingNull()
    {
        var entry = new FlightEntry
        {
            Timestamp = 12345L,
            Operation = "ping",
            Level = "info",
            V = "0.2.0"
            // All nullable fields are null
        };

        // Use UnityctlJsonContext (WhenWritingNull)
        var json = JsonSerializer.Serialize(entry, UnityctlJsonContext.Default.FlightEntry);

        Assert.DoesNotContain("\"project\"", json);
        Assert.DoesNotContain("\"transport\"", json);
        Assert.DoesNotContain("\"requestId\"", json);
        Assert.DoesNotContain("\"error\"", json);
        Assert.DoesNotContain("\"sid\"", json);
    }

    // ─── Round-trip ───────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = new FlightEntry
        {
            Timestamp = 1742313825000L,
            Operation = "build",
            Project = "/Users/x/MyGame",
            Transport = "ipc",
            StatusCode = 0,
            DurationMs = 123,
            RequestId = "req-abc",
            Level = "info",
            ExitCode = 0,
            Error = null,
            UnityVersion = "2022.3.1f1",
            Machine = "DEV-PC",
            V = "0.2.0",
            Args = "{\"target\":\"Android\"}",
            Sid = "session-xyz"
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<FlightEntry>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Timestamp, restored!.Timestamp);
        Assert.Equal(original.Operation, restored.Operation);
        Assert.Equal(original.Project, restored.Project);
        Assert.Equal(original.Transport, restored.Transport);
        Assert.Equal(original.StatusCode, restored.StatusCode);
        Assert.Equal(original.DurationMs, restored.DurationMs);
        Assert.Equal(original.RequestId, restored.RequestId);
        Assert.Equal(original.Level, restored.Level);
        Assert.Equal(original.ExitCode, restored.ExitCode);
        Assert.Equal(original.UnityVersion, restored.UnityVersion);
        Assert.Equal(original.Machine, restored.Machine);
        Assert.Equal(original.V, restored.V);
        Assert.Equal(original.Args, restored.Args);
        Assert.Equal(original.Sid, restored.Sid);
    }

    [Fact]
    public void Deserialization_FromCompactJson_Works()
    {
        // Simulates reading a line from an NDJSON file
        var json = "{\"ts\":1742313825000,\"op\":\"build\",\"project\":\"/proj\",\"statusCode\":0,\"durationMs\":99,\"level\":\"info\",\"v\":\"0.2.0\"}";

        var entry = JsonSerializer.Deserialize<FlightEntry>(json);

        Assert.NotNull(entry);
        Assert.Equal(1742313825000L, entry!.Timestamp);
        Assert.Equal("build", entry.Operation);
        Assert.Equal("/proj", entry.Project);
        Assert.Equal("info", entry.Level);
        Assert.Equal("0.2.0", entry.V);
    }

    [Fact]
    public void FlightEntryArray_SerializationRoundTrip()
    {
        var entries = new FlightEntry[]
        {
            new() { Timestamp = 1L, Operation = "build", V = "0.2.0" },
            new() { Timestamp = 2L, Operation = "test", Level = "error", V = "0.2.0" }
        };

        var json = JsonSerializer.Serialize(entries, UnityctlJsonContext.Default.FlightEntryArray);
        var restored = JsonSerializer.Deserialize<FlightEntry[]>(json);

        Assert.NotNull(restored);
        Assert.Equal(2, restored!.Length);
        Assert.Equal("build", restored[0].Operation);
        Assert.Equal("error", restored[1].Level);
    }
}
