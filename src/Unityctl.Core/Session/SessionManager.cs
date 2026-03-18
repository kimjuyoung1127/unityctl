using System.Diagnostics;
using System.Text.Json.Nodes;
using Unityctl.Shared.Protocol;

namespace Unityctl.Core.Sessions;

/// <summary>
/// Manages command execution session lifecycle.
/// Phase 3A: state machine transitions, stale detection, TTL cleanup.
/// </summary>
public sealed class SessionManager
{
    private readonly ISessionStore _store;
    private readonly Func<int, bool> _isProcessAlive;

    /// <param name="store">Session persistence backend.</param>
    /// <param name="isProcessAlive">Override for testability. Defaults to Process.GetProcessById check.</param>
    public SessionManager(ISessionStore store, Func<int, bool>? isProcessAlive = null)
    {
        _store = store;
        _isProcessAlive = isProcessAlive ?? DefaultIsAlive;
    }

    /// <summary>Create and persist a Running session.</summary>
    public async Task<Session> StartAsync(
        string command,
        string projectPath,
        string? transport = null,
        string? pipeName = null,
        int? unityPid = null,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        var session = new Session
        {
            Id = Guid.NewGuid().ToString("N"),
            State = SessionState.Running,
            Command = command,
            ProjectPath = projectPath,
            Transport = transport,
            PipeName = pipeName,
            UnityPid = unityPid,
            CliPid = Environment.ProcessId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _store.SaveAsync(session, ct);
        return session;
    }

    /// <summary>Transition a Running session to Completed.</summary>
    public async Task<Session> CompleteAsync(
        string sessionId,
        JsonObject? result = null,
        CancellationToken ct = default)
    {
        var session = await GetOrThrowAsync(sessionId, ct);
        ThrowIfNotState(session, SessionState.Running);

        var now = DateTimeOffset.UtcNow;
        session.State = SessionState.Completed;
        session.UpdatedAt = now.ToString("O");
        session.DurationMs = ComputeDurationMs(session.CreatedAt, now);
        session.Result = result;

        await _store.SaveAsync(session, ct);
        return session;
    }

    /// <summary>Transition a Running session to Failed.</summary>
    public async Task<Session> FailAsync(
        string sessionId,
        string errorMessage,
        CancellationToken ct = default)
    {
        var session = await GetOrThrowAsync(sessionId, ct);
        ThrowIfNotState(session, SessionState.Running);

        var now = DateTimeOffset.UtcNow;
        session.State = SessionState.Failed;
        session.UpdatedAt = now.ToString("O");
        session.DurationMs = ComputeDurationMs(session.CreatedAt, now);
        session.ErrorMessage = errorMessage;

        await _store.SaveAsync(session, ct);
        return session;
    }

    /// <summary>Transition a Running or Created session to Cancelled.</summary>
    public async Task<Session> CancelAsync(string sessionId, CancellationToken ct = default)
    {
        var session = await GetOrThrowAsync(sessionId, ct);
        if (session.State != SessionState.Running && session.State != SessionState.Created)
            throw new InvalidOperationException(
                $"Session {sessionId} is in state {session.State}, expected Running or Created.");

        session.State = SessionState.Cancelled;
        session.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        await _store.SaveAsync(session, ct);
        return session;
    }

    /// <summary>Transition a Running session to TimedOut.</summary>
    public async Task<Session> TimeoutAsync(string sessionId, CancellationToken ct = default)
    {
        var session = await GetOrThrowAsync(sessionId, ct);
        ThrowIfNotState(session, SessionState.Running);

        session.State = SessionState.TimedOut;
        session.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        await _store.SaveAsync(session, ct);
        return session;
    }

    /// <summary>List active sessions and recent history.</summary>
    public Task<IReadOnlyList<Session>> ListAsync(CancellationToken ct = default)
        => _store.ListAsync(ct);

    /// <summary>
    /// Mark stale sessions (CLI process exited) as Failed, then prune TTL-expired history.
    /// Returns total number of records cleaned.
    /// </summary>
    public async Task<int> CleanStaleAsync(CancellationToken ct = default)
    {
        var sessions = await _store.ListAsync(ct);
        var cleaned = 0;

        foreach (var session in sessions)
        {
            if (session.State != SessionState.Running && session.State != SessionState.Created)
                continue;

            if (session.CliPid.HasValue && !_isProcessAlive(session.CliPid.Value))
            {
                session.State = SessionState.Failed;
                session.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");
                session.ErrorMessage = "Stale: CLI process exited";
                await _store.SaveAsync(session, ct);
                cleaned++;
            }
        }

        cleaned += await _store.CleanupAsync(ct);
        return cleaned;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Session> GetOrThrowAsync(string sessionId, CancellationToken ct)
    {
        var session = await _store.GetAsync(sessionId, ct);
        return session ?? throw new InvalidOperationException($"Session '{sessionId}' not found.");
    }

    private static void ThrowIfNotState(Session session, SessionState expected)
    {
        if (session.State != expected)
            throw new InvalidOperationException(
                $"Session '{session.Id}' is in state {session.State}, expected {expected}.");
    }

    private static long ComputeDurationMs(string createdAt, DateTimeOffset now)
    {
        if (DateTimeOffset.TryParse(createdAt, out var ca))
            return (long)(now - ca).TotalMilliseconds;
        return 0;
    }

    private static bool DefaultIsAlive(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch
        {
            // Process not found or access denied → treat as dead
            return false;
        }
    }
}
