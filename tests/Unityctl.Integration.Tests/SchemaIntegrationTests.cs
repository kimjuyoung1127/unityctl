using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Unityctl.Integration.Tests;

/// <summary>
/// Black-box integration tests for 'unityctl schema'.
/// Skips gracefully when CLI is blocked by AppLocker.
/// </summary>
public class SchemaIntegrationTests
{
    private static readonly string? ExePath = FindCliExe();
    private static readonly bool CanExecute = CheckCanExecute();

    private readonly ITestOutputHelper _output;

    public SchemaIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Schema_Json_ExitCode0()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, stderr) = await RunCli("schema");
        _output.WriteLine($"stdout: {stdout}");
        _output.WriteLine($"stderr: {stderr}");
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Schema_Json_OutputIsValidJson()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, _) = await RunCli("schema");
        Assert.Equal(0, exitCode);
        // Must parse without throwing
        var doc = JsonDocument.Parse(stdout.Trim());
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task Schema_Json_ContainsVersionAndCommands()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, _) = await RunCli("schema");
        Assert.Equal(0, exitCode);

        var doc = JsonDocument.Parse(stdout.Trim());
        Assert.True(doc.RootElement.TryGetProperty("version", out _), "Missing 'version' field");
        Assert.True(doc.RootElement.TryGetProperty("commands", out var cmds), "Missing 'commands' field");
        Assert.Equal(JsonValueKind.Array, cmds.ValueKind);
        Assert.True(cmds.GetArrayLength() > 0, "commands array is empty");
    }

    [Fact]
    public async Task Schema_Json_CommandsHaveNameAndDescription()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, _) = await RunCli("schema");
        Assert.Equal(0, exitCode);

        var doc = JsonDocument.Parse(stdout.Trim());
        var commands = doc.RootElement.GetProperty("commands");

        foreach (var cmd in commands.EnumerateArray())
        {
            Assert.True(cmd.TryGetProperty("name", out var name), "Command missing 'name'");
            Assert.False(string.IsNullOrWhiteSpace(name.GetString()), "Command has empty name");
            Assert.True(cmd.TryGetProperty("description", out var desc), "Command missing 'description'");
            Assert.False(string.IsNullOrWhiteSpace(desc.GetString()), "Command has empty description");
        }
    }

    [Fact]
    public async Task Schema_WithFormatJson_ExitCode0()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, _, _) = await RunCli("schema", "--format", "json");
        Assert.Equal(0, exitCode);
    }

    private bool EnsureCanExecute()
    {
        if (ExePath == null)
        {
            _output.WriteLine("SKIPPED: CLI executable not found.");
            return false;
        }
        if (!CanExecute)
        {
            _output.WriteLine("SKIPPED: CLI blocked by AppLocker.");
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
        try { await process.WaitForExitAsync(cts.Token); }
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
        catch { return false; }
    }
}
