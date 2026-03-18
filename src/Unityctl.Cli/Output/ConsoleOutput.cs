using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Cli.Output;

public static class ConsoleOutput
{
    public static void PrintResponse(CommandResponse response)
    {
        if (response.StatusCode == StatusCode.Accepted)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("ACCEPTED [104]");
            Console.ResetColor();
            if (!string.IsNullOrEmpty(response.Message))
                Console.Write($" — {response.Message}");
            Console.WriteLine();
        }
        else if (response.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("OK");
            Console.ResetColor();
            if (!string.IsNullOrEmpty(response.Message))
                Console.Write($" — {response.Message}");
            Console.WriteLine();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"FAIL [{response.StatusCode}]");
            Console.ResetColor();
            if (!string.IsNullOrEmpty(response.Message))
                Console.Write($" — {response.Message}");
            Console.WriteLine();
        }

        if (response.Data != null)
        {
            if (response.Data["checks"] is JsonArray checksArray)
            {
                PrintPreflightChecks(checksArray);
            }
            else
            {
                foreach (var prop in response.Data)
                {
                    Console.WriteLine($"  {prop.Key}: {prop.Value}");
                }
            }
        }

        if (response.Errors is { Count: > 0 })
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            foreach (var error in response.Errors)
            {
                Console.Error.WriteLine($"  ! {error}");
            }
            Console.ResetColor();
        }
    }

    public static void PrintPreflightChecks(JsonArray checksArray)
    {
        var checks = JsonSerializer.Deserialize(checksArray, UnityctlJsonContext.Default.PreflightCheckArray);
        if (checks == null || checks.Length == 0) return;

        Console.WriteLine();
        Console.WriteLine("Preflight Checks:");

        foreach (var check in checks)
        {
            var (prefix, color) = GetCheckStyle(check);

            Console.ForegroundColor = color;
            Console.Write($"  {prefix} [{check.Category.ToUpperInvariant()}] {check.Check}");
            Console.ResetColor();
            Console.Write($": {check.Message}");
            if (!string.IsNullOrEmpty(check.Details))
                Console.Write($" ({check.Details})");
            Console.WriteLine();
        }

        var errors = checks.Count(c => c.Category == "error" && !c.Passed);
        var warnings = checks.Count(c => c.Category == "warning" && !c.Passed);
        var passed = checks.Count(c => c.Passed);

        Console.WriteLine();
        Console.Write($"  {checks.Length} checks: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"{passed} passed");
        Console.ResetColor();

        if (errors > 0)
        {
            Console.Write(", ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{errors} errors");
            Console.ResetColor();
        }

        if (warnings > 0)
        {
            Console.Write(", ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{warnings} warnings");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    public static void PrintRecovery(StatusCode code)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        var hint = code switch
        {
            StatusCode.NotFound => "Tip: Is Unity installed? Run 'unityctl editor list' to check.",
            StatusCode.ProjectLocked => "Tip: Close the running Unity Editor or wait for it to finish.",
            StatusCode.LicenseError => "Tip: Activate your Unity license via Unity Hub.",
            StatusCode.PluginNotInstalled => "Tip: Run 'unityctl init --project <path>' to install the plugin.",
            _ => null
        };
        if (hint != null) Console.Error.WriteLine(hint);
        Console.ResetColor();
    }

    private static (string Prefix, ConsoleColor Color) GetCheckStyle(PreflightCheck check)
    {
        return (check.Category, check.Passed) switch
        {
            ("error", false) => ("✗", ConsoleColor.Red),
            ("error", true) => ("✓", ConsoleColor.Green),
            ("warning", false) => ("⚠", ConsoleColor.Yellow),
            ("warning", true) => ("✓", ConsoleColor.Green),
            ("info", _) => ("ℹ", ConsoleColor.Cyan),
            _ => ("·", ConsoleColor.Gray)
        };
    }
}
