using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Unityctl.Integration.Tests;

/// <summary>
/// Black-box integration tests for the session subcommand.
/// Follows the same AppLocker-skip pattern as CliIntegrationTests.
/// </summary>
public class SessionIntegrationTests
{
    private static readonly string? ExePath = FindCliExe();
    private static readonly bool CanExecute = CheckCanExecute();

    private readonly ITestOutputHelper _output;

    public SessionIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SessionList_ExitCode0()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, stderr) = await RunCli("session", "list");
        _output.WriteLine($"stdout: {stdout}");
        _output.WriteLine($"stderr: {stderr}");
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task SessionList_Json_ExitCode0_ValidJsonArray()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, _) = await RunCli("session", "list", "--json");
        Assert.Equal(0, exitCode);
        Assert.StartsWith("[", stdout.Trim());
    }

    [Fact]
    public async Task SessionClean_ExitCode0()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, stderr) = await RunCli("session", "clean");
        _output.WriteLine($"stdout: {stdout}");
        _output.WriteLine($"stderr: {stderr}");
        Assert.Equal(0, exitCode);
        Assert.Contains("Cleaned", stdout);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool EnsureCanExecute()
    {
        if (ExePath == null)
        {
            _output.WriteLine("SKIPPED: CLI executable not found. Build the solution first.");
            return false;
        }

        if (!CanExecute)
        {
            _output.WriteLine("SKIPPED: CLI executable blocked by application control policy.");
            return false;
        }

        return true;
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunCli(params string[] args)
    {
        if (ExePath == null)
            throw new InvalidOperationException("CLI executable not found");

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = ExePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException("CLI process did not exit within 30 seconds");
        }

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static string? FindCliExe()
    {
        var path = GetExpectedExePath();
        return File.Exists(path) ? path : null;
    }

    private static string GetExpectedExePath()
    {
        var baseDir = AppContext.BaseDirectory;
        var solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "unityctl.exe" : "unityctl";
        return Path.Combine(solutionDir, "src", "Unityctl.Cli", "bin", "Debug", "net10.0", exeName);
    }

    private static bool CheckCanExecute()
    {
        if (ExePath == null) return false;
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ExePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            process.Start();
            process.WaitForExit(10_000);
            return process.ExitCode != -532462766;
        }
        catch
        {
            return false;
        }
    }
}
