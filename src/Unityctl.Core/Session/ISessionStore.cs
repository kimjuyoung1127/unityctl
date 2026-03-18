using Unityctl.Shared.Protocol;

namespace Unityctl.Core.Sessions;

/// <summary>
/// Persistence contract for command execution sessions.
/// Phase 3A: active.json (running) + history.ndjson (terminal) storage.
/// </summary>
public interface ISessionStore
{
    /// <summary>Save or update a session. Terminal states move the record to history.</summary>
    Task SaveAsync(Session session, CancellationToken ct = default);

    /// <summary>Retrieve a session by ID. Searches active sessions first, then history.</summary>
    Task<Session?> GetAsync(string sessionId, CancellationToken ct = default);

    /// <summary>List all active sessions plus recent history (up to 50 entries).</summary>
    Task<IReadOnlyList<Session>> ListAsync(CancellationToken ct = default);

    /// <summary>Remove a session from the active set. Returns true if it was present.</summary>
    Task<bool> DeleteAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Prune history entries older than SessionTtlDays. Returns count removed.</summary>
    Task<int> CleanupAsync(CancellationToken ct = default);
}
