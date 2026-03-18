using System.Text.Json.Nodes;
using Unityctl.Core.Sessions;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Core.Tests.Sessions;

public sealed class SessionManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly NdjsonSessionStore _store;
    private readonly SessionManager _manager;

    public SessionManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"unityctl-mgr-test-{Guid.NewGuid():N}");
        _store = new NdjsonSessionStore(_tempDir);
        _manager = new SessionManager(_store);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // ─── Start ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Start_ReturnsRunningSession()
    {
        var session = await _manager.StartAsync("build", "/test/project");

        Assert.Equal(SessionState.Running, session.State);
        Assert.NotEmpty(session.Id);
        Assert.Equal("build", session.Command);
        Assert.Equal("/test/project", session.ProjectPath);
        Assert.NotEmpty(session.CreatedAt);
        Assert.Equal(Environment.ProcessId, session.CliPid);
    }

    [Fact]
    public async Task Start_PersistsSessionToStore()
    {
        var session = await _manager.StartAsync("test", "/proj");

        var found = await _store.GetAsync(session.Id);
        Assert.NotNull(found);
        Assert.Equal(session.Id, found.Id);
    }

    [Fact]
    public async Task Start_WithTransportAndPipe_SetsFields()
    {
        var session = await _manager.StartAsync("ping", "/proj", transport: "ipc", pipeName: "unityctl_abc123");

        Assert.Equal("ipc", session.Transport);
        Assert.Equal("unityctl_abc123", session.PipeName);
    }

    // ─── Complete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Complete_FromRunning_Succeeds()
    {
        var session = await _manager.StartAsync("build", "/proj");

        await Task.Delay(10); // ensure DurationMs > 0

        var completed = await _manager.CompleteAsync(session.Id);

        Assert.Equal(SessionState.Completed, completed.State);
        Assert.True(completed.DurationMs >= 0);
        Assert.NotNull(completed.UpdatedAt);
    }

    [Fact]
    public async Task Complete_WithResult_StoresResult()
    {
        var session = await _manager.StartAsync("build", "/proj");
        var result = new JsonObject { ["output"] = "Success" };

        var completed = await _manager.CompleteAsync(session.Id, result);

        Assert.NotNull(completed.Result);
    }

    [Fact]
    public async Task Complete_FromCompleted_Throws()
    {
        var session = await _manager.StartAsync("build", "/proj");
        await _manager.CompleteAsync(session.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.CompleteAsync(session.Id));
    }

    // ─── Fail ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Fail_FromRunning_Succeeds()
    {
        var session = await _manager.StartAsync("build", "/proj");

        var failed = await _manager.FailAsync(session.Id, "Build error occurred");

        Assert.Equal(SessionState.Failed, failed.State);
        Assert.Equal("Build error occurred", failed.ErrorMessage);
        Assert.True(failed.DurationMs >= 0);
    }

    [Fact]
    public async Task Fail_FromCompleted_Throws()
    {
        var session = await _manager.StartAsync("build", "/proj");
        await _manager.CompleteAsync(session.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.FailAsync(session.Id, "error"));
    }

    // ─── Cancel ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_FromRunning_Succeeds()
    {
        var session = await _manager.StartAsync("test", "/proj");

        var cancelled = await _manager.CancelAsync(session.Id);

        Assert.Equal(SessionState.Cancelled, cancelled.State);
    }

    [Fact]
    public async Task Cancel_FromCompleted_Throws()
    {
        var session = await _manager.StartAsync("test", "/proj");
        await _manager.CompleteAsync(session.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.CancelAsync(session.Id));
    }

    // ─── Timeout ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Timeout_FromRunning_Succeeds()
    {
        var session = await _manager.StartAsync("test", "/proj");

        var timedOut = await _manager.TimeoutAsync(session.Id);

        Assert.Equal(SessionState.TimedOut, timedOut.State);
    }

    [Fact]
    public async Task Timeout_FromFailed_Throws()
    {
        var session = await _manager.StartAsync("test", "/proj");
        await _manager.FailAsync(session.Id, "error");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.TimeoutAsync(session.Id));
    }

    // ─── List ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_ReturnsAllSessions()
    {
        await _manager.StartAsync("build", "/proj");
        await _manager.StartAsync("test", "/proj");

        var s3 = await _manager.StartAsync("ping", "/proj");
        await _manager.CompleteAsync(s3.Id);

        var list = await _manager.ListAsync();

        Assert.Equal(3, list.Count);
    }

    [Fact]
    public async Task List_Empty_ReturnsEmpty()
    {
        var list = await _manager.ListAsync();
        Assert.Empty(list);
    }

    // ─── Stale Detection ──────────────────────────────────────────────────────

    [Fact]
    public async Task CleanStale_DetectsDeadPid()
    {
        // Use a guaranteed-dead PID by injecting a custom isAlive function
        var storeForStale = new NdjsonSessionStore(_tempDir);
        var managerWithDeadPid = new SessionManager(storeForStale, _ => false);

        await managerWithDeadPid.StartAsync("build", "/proj");

        var cleaned = await managerWithDeadPid.CleanStaleAsync();

        Assert.True(cleaned >= 1);
        var list = await storeForStale.ListAsync();
        // All sessions should be failed now (moved to history or marked failed)
        Assert.All(list, s => Assert.NotEqual(SessionState.Running, s.State));
    }

    [Fact]
    public async Task CleanStale_SkipsLivePid()
    {
        // Current process is alive
        var storeForLive = new NdjsonSessionStore(_tempDir);
        var managerAlive = new SessionManager(storeForLive, _ => true);

        await managerAlive.StartAsync("build", "/proj");

        var cleaned = await managerAlive.CleanStaleAsync();

        // Only TTL cleanup, no stale sessions
        Assert.Equal(0, cleaned);
        var list = await storeForLive.ListAsync();
        Assert.Single(list);
        Assert.Equal(SessionState.Running, list[0].State);
    }

    [Fact]
    public async Task CleanStale_NotFoundSession_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.CompleteAsync("nonexistent-id"));
    }
}
