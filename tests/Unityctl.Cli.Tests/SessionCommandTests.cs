using System.Text.Json;
using Unityctl.Cli.Commands;
using Unityctl.Core.Sessions;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

[Xunit.Collection("ConsoleOutput")]
public sealed class SessionCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly NdjsonSessionStore _store;
    private readonly SessionManager _manager;

    public SessionCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"unityctl-session-cmd-{Guid.NewGuid():N}");
        _store = new NdjsonSessionStore(_tempDir);
        _manager = new SessionManager(_store);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static string CaptureConsole(Action action)
    {
        var prev = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try { action(); return sw.ToString(); }
        finally { Console.SetOut(prev); }
    }

    // ─── List ─────────────────────────────────────────────────────────────────

    [CliTestFact]
    public void List_Empty_PrintsNoActiveSessions()
    {
        var output = CaptureConsole(() =>
            SessionCommand.ListCore(_manager, json: false));

        Assert.Contains("No active sessions", output);
    }

    [CliTestFact]
    public void List_Json_Empty_PrintsJsonArray()
    {
        var output = CaptureConsole(() =>
            SessionCommand.ListCore(_manager, json: true));

        var trimmed = output.Trim();
        Assert.StartsWith("[", trimmed);
        Assert.EndsWith("]", trimmed);

        var sessions = JsonSerializer.Deserialize<Session[]>(trimmed);
        Assert.NotNull(sessions);
        Assert.Empty(sessions);
    }

    [CliTestFact]
    public async Task List_WithSessions_PrintsTable()
    {
        await _manager.StartAsync("build", "/my/project");

        var output = CaptureConsole(() =>
            SessionCommand.ListCore(_manager, json: false));

        Assert.Contains("build", output);
        Assert.Contains("Running", output);
    }

    [CliTestFact]
    public async Task List_Json_WithSessions_PrintsJsonArray()
    {
        await _manager.StartAsync("test", "/proj");

        var output = CaptureConsole(() =>
            SessionCommand.ListCore(_manager, json: true));

        var trimmed = output.Trim();
        Assert.StartsWith("[", trimmed);

        var sessions = JsonSerializer.Deserialize<Session[]>(trimmed);
        Assert.NotNull(sessions);
        Assert.Single(sessions);
        Assert.Equal("test", sessions![0].Command);
    }

    // ─── Stop ─────────────────────────────────────────────────────────────────

    [CliTestFact]
    public async Task Stop_ValidId_PrintsCancelledMessage()
    {
        var session = await _manager.StartAsync("build", "/proj");

        var output = CaptureConsole(() =>
            SessionCommand.StopCore(_manager, session.Id, json: false));

        Assert.Contains("cancelled", output.ToLowerInvariant());
    }

    [CliTestFact]
    public async Task Stop_ValidId_Json_PrintsSuccess()
    {
        var session = await _manager.StartAsync("build", "/proj");

        var output = CaptureConsole(() =>
            SessionCommand.StopCore(_manager, session.Id, json: true));

        Assert.Contains("\"success\":true", output);
        Assert.Contains(session.Id, output);
    }

    // ─── Clean ────────────────────────────────────────────────────────────────

    [CliTestFact]
    public void Clean_NothingToClean_PrintsZero()
    {
        var output = CaptureConsole(() =>
            SessionCommand.CleanCore(_manager));

        Assert.Contains("Cleaned", output);
        Assert.Contains("0", output);
    }

    [CliTestFact]
    public async Task Clean_WithDeadSessions_PrintsCount()
    {
        var deadManager = new SessionManager(_store, _ => false);
        await deadManager.StartAsync("build", "/proj");

        var output = CaptureConsole(() =>
            SessionCommand.CleanCore(deadManager));

        Assert.Contains("Cleaned", output);
        Assert.Contains("1", output);
    }
}
