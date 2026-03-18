using System.Text.Json;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;

namespace Unityctl.Core.Sessions;

/// <summary>
/// NDJSON-backed session store.
/// Active sessions: ~/.unityctl/sessions/active.json (JSON array, overwrite)
/// Completed/failed history: ~/.unityctl/sessions/history.ndjson (append-only)
/// Thread-safe within a single process via lock.
/// </summary>
public sealed class NdjsonSessionStore : ISessionStore
{
    private readonly string _sessionsDir;
    private readonly string _activePath;
    private readonly string _historyPath;
    private readonly object _writeLock = new();

    public NdjsonSessionStore(string? baseDirectory = null)
    {
        _sessionsDir = baseDirectory ??
            Path.Combine(Constants.GetConfigDirectory(), Constants.SessionsDirectory);
        _activePath = Path.Combine(_sessionsDir, Constants.SessionActiveFile);
        _historyPath = Path.Combine(_sessionsDir, Constants.SessionHistoryFile);
    }

    public Task SaveAsync(Session session, CancellationToken ct = default)
    {
        lock (_writeLock)
        {
            Directory.CreateDirectory(_sessionsDir);

            var activeSessions = ReadActiveSessions();

            if (IsTerminalState(session.State))
            {
                // Remove from active, append to history
                activeSessions.RemoveAll(s => s.Id == session.Id);
                WriteActiveSessions(activeSessions);

                var line = JsonSerializer.Serialize(session, SessionJsonContext.Default.Session);
                File.AppendAllText(_historyPath, line + "\n");
            }
            else
            {
                // Update or add to active list
                var idx = activeSessions.FindIndex(s => s.Id == session.Id);
                if (idx >= 0)
                    activeSessions[idx] = session;
                else
                    activeSessions.Add(session);

                WriteActiveSessions(activeSessions);
            }
        }

        return Task.CompletedTask;
    }

    public Task<Session?> GetAsync(string sessionId, CancellationToken ct = default)
    {
        lock (_writeLock)
        {
            // Search active first
            var found = ReadActiveSessions().Find(s => s.Id == sessionId);
            if (found != null)
                return Task.FromResult<Session?>(found);

            // Scan history in reverse (newest first)
            if (File.Exists(_historyPath))
            {
                var lines = File.ReadAllLines(_historyPath);
                for (var i = lines.Length - 1; i >= 0; i--)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var s = JsonSerializer.Deserialize(line, SessionJsonContext.Default.Session);
                        if (s?.Id == sessionId)
                            return Task.FromResult<Session?>(s);
                    }
                    catch { }
                }
            }
        }

        return Task.FromResult<Session?>(null);
    }

    public Task<IReadOnlyList<Session>> ListAsync(CancellationToken ct = default)
    {
        lock (_writeLock)
        {
            var result = new List<Session>(ReadActiveSessions());

            // Append recent history (newest first, up to 50)
            if (File.Exists(_historyPath))
            {
                var lines = File.ReadAllLines(_historyPath);
                var added = 0;
                for (var i = lines.Length - 1; i >= 0 && added < 50; i--)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var s = JsonSerializer.Deserialize(line, SessionJsonContext.Default.Session);
                        if (s != null)
                        {
                            result.Add(s);
                            added++;
                        }
                    }
                    catch { }
                }
            }

            return Task.FromResult<IReadOnlyList<Session>>(result);
        }
    }

    public Task<bool> DeleteAsync(string sessionId, CancellationToken ct = default)
    {
        lock (_writeLock)
        {
            var activeSessions = ReadActiveSessions();
            var removed = activeSessions.RemoveAll(s => s.Id == sessionId) > 0;
            if (removed)
                WriteActiveSessions(activeSessions);
            return Task.FromResult(removed);
        }
    }

    public Task<int> CleanupAsync(CancellationToken ct = default)
    {
        lock (_writeLock)
        {
            if (!File.Exists(_historyPath))
                return Task.FromResult(0);

            var cutoff = DateTimeOffset.UtcNow.AddDays(-Constants.SessionTtlDays);
            var lines = File.ReadAllLines(_historyPath);
            var kept = new List<string>();
            var removed = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var shouldRemove = false;
                try
                {
                    var s = JsonSerializer.Deserialize(line, SessionJsonContext.Default.Session);
                    if (s != null &&
                        DateTimeOffset.TryParse(s.CreatedAt, out var createdAt) &&
                        createdAt < cutoff)
                    {
                        shouldRemove = true;
                    }
                }
                catch { }

                if (shouldRemove)
                    removed++;
                else
                    kept.Add(line);
            }

            if (removed > 0)
            {
                var tempPath = _historyPath + ".tmp";
                File.WriteAllLines(tempPath, kept);
                File.Move(tempPath, _historyPath, overwrite: true);
            }

            return Task.FromResult(removed);
        }
    }

    private List<Session> ReadActiveSessions()
    {
        if (!File.Exists(_activePath))
            return [];

        try
        {
            var json = File.ReadAllText(_activePath);
            if (string.IsNullOrWhiteSpace(json))
                return [];

            var sessions = JsonSerializer.Deserialize(json, SessionJsonContext.Default.SessionArray);
            return sessions != null ? [.. sessions] : [];
        }
        catch
        {
            return [];
        }
    }

    private void WriteActiveSessions(List<Session> sessions)
    {
        var json = JsonSerializer.Serialize(sessions.ToArray(), SessionJsonContext.Default.SessionArray);
        File.WriteAllText(_activePath, json);
    }

    private static bool IsTerminalState(SessionState state) =>
        state is SessionState.Completed or SessionState.Failed
            or SessionState.Cancelled or SessionState.TimedOut;
}
