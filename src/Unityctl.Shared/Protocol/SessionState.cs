namespace Unityctl.Shared.Protocol;

/// <summary>
/// Session lifecycle state machine.
/// Phase 3A: 6 states covering the full command execution lifecycle.
/// </summary>
public enum SessionState
{
    Created = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    TimedOut = 5
}
