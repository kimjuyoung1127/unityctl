using Unityctl.Core.Platform;

namespace Unityctl.Core.Discovery;

/// <summary>
/// Detects running Unity Editor processes and their associated projects.
/// Phase 2B: WMI (Windows) / ps (macOS/Linux) based detection.
/// </summary>
public sealed class UnityProcessDetector
{
    private readonly IPlatformServices _platform;

    public UnityProcessDetector(IPlatformServices platform)
    {
        _platform = platform;
    }

    /// <summary>
    /// Check if a Unity Editor is running for the given project.
    /// </summary>
    public bool IsEditorRunning(string projectPath)
    {
        var normalized = Path.GetFullPath(projectPath);
        return _platform.FindRunningUnityProcesses()
            .Any(p => string.Equals(p.ProjectPath, normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Find the Unity process for a specific project.
    /// </summary>
    public UnityProcessInfo? FindProcessForProject(string projectPath)
    {
        var normalized = Path.GetFullPath(projectPath);
        return _platform.FindRunningUnityProcesses()
            .FirstOrDefault(p => string.Equals(p.ProjectPath, normalized, StringComparison.OrdinalIgnoreCase));
    }
}
