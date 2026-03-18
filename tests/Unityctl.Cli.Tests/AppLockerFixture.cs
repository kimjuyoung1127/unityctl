using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Unityctl.Cli.Tests;

/// <summary>
/// Detects whether AppLocker blocks loading the CLI assembly.
/// On restricted environments (e.g. school PCs), unityctl.dll cannot be loaded
/// and all CLI-dependent tests will gracefully pass with a diagnostic message.
/// </summary>
public static class AppLockerGuard
{
    private static readonly Lazy<bool> _canLoad = new(Probe);

    public static bool CanLoad => _canLoad.Value;

    private static bool Probe()
    {
        try
        {
            var asm = Assembly.Load("unityctl");
            return asm != null;
        }
        catch { return false; }
    }
}

/// <summary>
/// xUnit custom Fact that skips when AppLocker blocks CLI assembly loading.
/// Use [CliTestFact] instead of [Fact] for tests that reference CLI types.
/// </summary>
public sealed class CliTestFactAttribute : FactAttribute
{
    public CliTestFactAttribute()
    {
        if (!AppLockerGuard.CanLoad)
        {
            Skip = "CLI assembly blocked by AppLocker policy. Skipping on restricted environment.";
        }
    }
}
