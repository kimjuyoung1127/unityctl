using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Cli.Tests;

[Collection("ConsoleOutput")]
public sealed class WatchCommandTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string CaptureStdout(Action action)
    {
        var prev = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try { action(); return sw.ToString(); }
        finally { Console.SetOut(prev); }
    }

    private static EventEnvelope MakeEnvelope(
        string channel,
        string eventType,
        long? timestamp = null,
        JsonObject? payload = null)
    {
        return new EventEnvelope
        {
            Channel = channel,
            EventType = eventType,
            Timestamp = timestamp ?? 1_700_000_000_000L, // 2023-11-14T22:13:20.000Z
            Payload = payload
        };
    }

    // ─── Text Format Tests ────────────────────────────────────────────────────

    [CliTestFact]
    public void TextFormat_IncludesTimestamp()
    {
        var evt = MakeEnvelope("console", "Log", timestamp: 1_700_000_000_000L);
        var output = CaptureStdout(() => WatchCommand.PrintEvent(evt, "text", noColor: true));
        // Check that the time portion exists in HH:mm:ss.fff format (not empty)
        Assert.Matches(@"\d{2}:\d{2}:\d{2}\.\d{3}", output);
    }

    [CliTestFact]
    public void TextFormat_IncludesChannelAndEventType()
    {
        var evt = MakeEnvelope("console", "Log");
        var output = CaptureStdout(() => WatchCommand.PrintEvent(evt, "text", noColor: true));
        Assert.Contains("[console/Log]", output);
    }

    [CliTestFact]
    public void TextFormat_HierarchyEvent_IncludesTag()
    {
        var evt = MakeEnvelope("hierarchy", "Changed");
        var output = CaptureStdout(() => WatchCommand.PrintEvent(evt, "text", noColor: true));
        Assert.Contains("[hierarchy/Changed]", output);
    }

    [CliTestFact]
    public void TextFormat_WithPayloadMessage_PrintsMessage()
    {
        var payload = new JsonObject { ["message"] = "NullRefException thrown" };
        var evt = MakeEnvelope("console", "Error", payload: payload);
        var output = CaptureStdout(() => WatchCommand.PrintEvent(evt, "text", noColor: true));
        Assert.Contains("NullRefException thrown", output);
    }

    [CliTestFact]
    public void TextFormat_NoPayload_PrintsEventType()
    {
        var evt = MakeEnvelope("hierarchy", "Changed");
        var output = CaptureStdout(() => WatchCommand.PrintEvent(evt, "text", noColor: true));
        Assert.Contains("Changed", output);
    }

    // ─── JSON Format Tests ────────────────────────────────────────────────────

    [CliTestFact]
    public void JsonFormat_OutputIsValidJson()
    {
        var evt = MakeEnvelope("console", "Log");
        var output = CaptureStdout(() => WatchCommand.PrintEvent(evt, "json", noColor: true));
        var trimmed = output.Trim();
        // Must parse without exception
        using var doc = JsonDocument.Parse(trimmed);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [CliTestFact]
    public void JsonFormat_ContainsChannelField()
    {
        var evt = MakeEnvelope("compilation", "Started");
        var output = CaptureStdout(() => WatchCommand.PrintEvent(evt, "json", noColor: true));
        using var doc = JsonDocument.Parse(output.Trim());
        Assert.Equal("compilation", doc.RootElement.GetProperty("channel").GetString());
    }

    [CliTestFact]
    public void JsonFormat_ContainsEventTypeField()
    {
        var evt = MakeEnvelope("compilation", "Finished");
        var output = CaptureStdout(() => WatchCommand.PrintEvent(evt, "json", noColor: true));
        using var doc = JsonDocument.Parse(output.Trim());
        Assert.Equal("Finished", doc.RootElement.GetProperty("eventType").GetString());
    }

    [CliTestFact]
    public void JsonFormat_ContainsTimestampField()
    {
        var evt = MakeEnvelope("console", "Warning", timestamp: 9_999_000L);
        var output = CaptureStdout(() => WatchCommand.PrintEvent(evt, "json", noColor: true));
        using var doc = JsonDocument.Parse(output.Trim());
        Assert.Equal(9_999_000L, doc.RootElement.GetProperty("timestamp").GetInt64());
    }

    // ─── Color Selection Tests ────────────────────────────────────────────────

    [CliTestFact]
    public void GetEventColor_ErrorEvent_ReturnsRed()
    {
        var evt = MakeEnvelope("console", "Error");
        Assert.Equal(ConsoleColor.Red, WatchCommand.GetEventColor(evt));
    }

    [CliTestFact]
    public void GetEventColor_ExceptionEvent_ReturnsRed()
    {
        var evt = MakeEnvelope("console", "Exception");
        Assert.Equal(ConsoleColor.Red, WatchCommand.GetEventColor(evt));
    }

    [CliTestFact]
    public void GetEventColor_WarningEvent_ReturnsYellow()
    {
        var evt = MakeEnvelope("console", "Warning");
        Assert.Equal(ConsoleColor.Yellow, WatchCommand.GetEventColor(evt));
    }

    [CliTestFact]
    public void GetEventColor_LogEvent_ReturnsWhite()
    {
        var evt = MakeEnvelope("console", "Log");
        Assert.Equal(ConsoleColor.White, WatchCommand.GetEventColor(evt));
    }

    [CliTestFact]
    public void GetEventColor_HeartbeatChannel_ReturnsDarkGray()
    {
        var evt = MakeEnvelope("_heartbeat", "Ping");
        Assert.Equal(ConsoleColor.DarkGray, WatchCommand.GetEventColor(evt));
    }

    [CliTestFact]
    public void GetEventColor_OverflowChannel_ReturnsDarkGray()
    {
        var evt = MakeEnvelope("_overflow", "Dropped");
        Assert.Equal(ConsoleColor.DarkGray, WatchCommand.GetEventColor(evt));
    }

    // ─── ExtractMessage Tests ─────────────────────────────────────────────────

    [CliTestFact]
    public void ExtractMessage_PayloadHasMessage_ReturnsMessage()
    {
        var payload = new JsonObject { ["message"] = "Script compiled" };
        var evt = MakeEnvelope("compilation", "Finished", payload: payload);
        Assert.Equal("Script compiled", WatchCommand.ExtractMessage(evt));
    }

    [CliTestFact]
    public void ExtractMessage_NoPayload_ReturnsEventType()
    {
        var evt = MakeEnvelope("hierarchy", "Changed");
        Assert.Equal("Changed", WatchCommand.ExtractMessage(evt));
    }

    [CliTestFact]
    public void ExtractMessage_PayloadMissingMessageKey_ReturnsEventType()
    {
        var payload = new JsonObject { ["otherKey"] = "value" };
        var evt = MakeEnvelope("console", "Log", payload: payload);
        Assert.Equal("Log", WatchCommand.ExtractMessage(evt));
    }

    // ─── NoColor Tests ────────────────────────────────────────────────────────

    [CliTestFact]
    public void NoColor_DoesNotChangeConsoleForegroundColor()
    {
        var evt = MakeEnvelope("console", "Error");
        var colorBefore = Console.ForegroundColor;

        CaptureStdout(() => WatchCommand.PrintEvent(evt, "text", noColor: true));

        Assert.Equal(colorBefore, Console.ForegroundColor);
    }

    [CliTestFact]
    public void TextFormat_OutputEndsWithNewline()
    {
        var evt = MakeEnvelope("console", "Log");
        var output = CaptureStdout(() => WatchCommand.PrintEvent(evt, "text", noColor: true));
        Assert.EndsWith(Environment.NewLine, output);
    }

    [CliTestFact]
    public void JsonFormat_OutputEndsWithNewline()
    {
        var evt = MakeEnvelope("console", "Log");
        var output = CaptureStdout(() => WatchCommand.PrintEvent(evt, "json", noColor: true));
        Assert.EndsWith(Environment.NewLine, output);
    }
}
