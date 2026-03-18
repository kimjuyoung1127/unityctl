using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Core.Sessions;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Core.Tests.Sessions;

public sealed class NdjsonSessionStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly NdjsonSessionStore _store;

    public NdjsonSessionStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"unityctl-session-test-{Guid.NewGuid():N}");
        _store = new NdjsonSessionStore(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static Session MakeSession(
        string? id = null,
        SessionState state = SessionState.Running,
        string command = "build",
        string project = "/test/project") => new()
    {
        Id = id ?? Guid.NewGuid().ToString("N"),
        State = state,
        Command = command,
        ProjectPath = project,
        CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
        UpdatedAt = DateTimeOffset.UtcNow.ToString("O"),
        CliPid = Environment.ProcessId
    };

    // ─── Save/Get Active ──────────────────────────────────────────────────────

    [Fact]
    public async Task Save_RunningSession_WritesToActiveJson()
    {
        var session = MakeSession(state: SessionState.Running);
        await _store.SaveAsync(session);

        var activePath = Path.Combine(_tempDir, "active.json");
        Assert.True(File.Exists(activePath));

        var json = File.ReadAllText(activePath);
        Assert.Contains(session.Id, json);
    }

    [Fact]
    public async Task Save_CompletedSession_MovesToHistoryNdjson()
    {
        var session = MakeSession(state: SessionState.Running);
        await _store.SaveAsync(session);

        session.State = SessionState.Completed;
        await _store.SaveAsync(session);

        var historyPath = Path.Combine(_tempDir, "history.ndjson");
        Assert.True(File.Exists(historyPath));

        var lines = File.ReadAllLines(historyPath)
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        Assert.Single(lines);
        Assert.Contains(session.Id, lines[0]);

        // Should be removed from active
        var activePath = Path.Combine(_tempDir, "active.json");
        var activeJson = File.ReadAllText(activePath);
        Assert.DoesNotContain(session.Id, activeJson);
    }

    [Fact]
    public async Task Save_FailedSession_MovesToHistory()
    {
        var session = MakeSession(state: SessionState.Running);
        await _store.SaveAsync(session);
        session.State = SessionState.Failed;
        session.ErrorMessage = "Something went wrong";
        await _store.SaveAsync(session);

        var historyPath = Path.Combine(_tempDir, "history.ndjson");
        Assert.True(File.Exists(historyPath));
        Assert.Contains(session.Id, File.ReadAllText(historyPath));
    }

    [Fact]
    public async Task Save_CreatesDirectoryIfMissing()
    {
        Assert.False(Directory.Exists(_tempDir));
        var session = MakeSession();
        await _store.SaveAsync(session);
        Assert.True(Directory.Exists(_tempDir));
    }

    // ─── Get ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ExistingActiveSession_ReturnsSession()
    {
        var session = MakeSession();
        await _store.SaveAsync(session);

        var found = await _store.GetAsync(session.Id);

        Assert.NotNull(found);
        Assert.Equal(session.Id, found.Id);
        Assert.Equal(session.Command, found.Command);
    }

    [Fact]
    public async Task Get_SessionInHistory_ReturnsSession()
    {
        var session = MakeSession(state: SessionState.Running);
        await _store.SaveAsync(session);
        session.State = SessionState.Completed;
        await _store.SaveAsync(session);

        var found = await _store.GetAsync(session.Id);

        Assert.NotNull(found);
        Assert.Equal(session.Id, found.Id);
        Assert.Equal(SessionState.Completed, found.State);
    }

    [Fact]
    public async Task Get_NonExistentId_ReturnsNull()
    {
        var found = await _store.GetAsync("nonexistent-id-xyz");
        Assert.Null(found);
    }

    // ─── List ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_ActiveAndHistory_MergesBoth()
    {
        var active = MakeSession(command: "build");
        await _store.SaveAsync(active);

        var historical = MakeSession(command: "test", state: SessionState.Running);
        await _store.SaveAsync(historical);
        historical.State = SessionState.Completed;
        await _store.SaveAsync(historical);

        var list = await _store.ListAsync();

        Assert.Equal(2, list.Count);
        Assert.Contains(list, s => s.Id == active.Id);
        Assert.Contains(list, s => s.Id == historical.Id);
    }

    [Fact]
    public async Task List_Empty_ReturnsEmptyList()
    {
        var list = await _store.ListAsync();
        Assert.Empty(list);
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingActiveSession_ReturnsTrue()
    {
        var session = MakeSession();
        await _store.SaveAsync(session);

        var removed = await _store.DeleteAsync(session.Id);

        Assert.True(removed);
        var found = await _store.GetAsync(session.Id);
        Assert.Null(found);
    }

    [Fact]
    public async Task Delete_NonExistentId_ReturnsFalse()
    {
        var removed = await _store.DeleteAsync("does-not-exist");
        Assert.False(removed);
    }

    // ─── Cleanup ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cleanup_RemovesExpiredEntries()
    {
        // Manually write an old entry to history.ndjson
        Directory.CreateDirectory(_tempDir);
        var historyPath = Path.Combine(_tempDir, "history.ndjson");
        var oldSession = MakeSession(state: SessionState.Completed);
        oldSession.CreatedAt = DateTimeOffset.UtcNow.AddDays(-8).ToString("O");
        var oldLine = System.Text.Json.JsonSerializer.Serialize(oldSession);
        File.WriteAllText(historyPath, oldLine + "\n");

        var removed = await _store.CleanupAsync();

        Assert.Equal(1, removed);
        var remaining = File.ReadAllLines(historyPath)
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Cleanup_RecentEntries_AreNotRemoved()
    {
        var session = MakeSession(state: SessionState.Running);
        await _store.SaveAsync(session);
        session.State = SessionState.Completed;
        await _store.SaveAsync(session);

        var removed = await _store.CleanupAsync();

        Assert.Equal(0, removed);
        var found = await _store.GetAsync(session.Id);
        Assert.NotNull(found);
    }

    [Fact]
    public async Task Cleanup_NoHistoryFile_ReturnsZero()
    {
        var removed = await _store.CleanupAsync();
        Assert.Equal(0, removed);
    }

    // ─── Multiple Sessions ────────────────────────────────────────────────────

    [Fact]
    public async Task Save_MultipleActiveSessions_AllInActiveJson()
    {
        var s1 = MakeSession(command: "build");
        var s2 = MakeSession(command: "test");
        var s3 = MakeSession(command: "ping");

        await _store.SaveAsync(s1);
        await _store.SaveAsync(s2);
        await _store.SaveAsync(s3);

        var list = await _store.ListAsync();

        Assert.Equal(3, list.Count);
    }
}
