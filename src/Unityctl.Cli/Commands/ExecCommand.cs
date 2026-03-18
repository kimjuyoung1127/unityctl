using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

/// <summary>
/// Executes a C# expression inside the Unity Editor via reflection.
/// The Editor plugin handles evaluation; this command just sends the request.
/// </summary>
public static class ExecCommand
{
    public static void Execute(string project, string? code = null, string? file = null, bool json = false)
    {
        var resolvedCode = ResolveCode(code, file);
        if (resolvedCode == null)
        {
            Console.Error.WriteLine("Error: Provide --code <expression> or --file <path>.");
            Environment.Exit(1);
            return;
        }

        var request = CreateRequest(resolvedCode);
        CommandRunner.Execute(project, request, json);
    }

    /// <summary>
    /// Resolves the C# code string from either inline --code or --file argument.
    /// Returns null if neither is provided.
    /// </summary>
    internal static string? ResolveCode(string? code, string? file)
    {
        if (!string.IsNullOrWhiteSpace(file))
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"Error: File not found: {file}");
                Environment.Exit(1);
                return null;
            }

            return File.ReadAllText(file);
        }

        return string.IsNullOrWhiteSpace(code) ? null : code;
    }

    /// <summary>
    /// Creates an exec CommandRequest from a C# code string. Exposed for testing.
    /// </summary>
    internal static CommandRequest CreateRequest(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("code must not be empty", nameof(code));

        return new CommandRequest
        {
            Command = WellKnownCommands.Exec,
            Parameters = new JsonObject { ["code"] = code }
        };
    }
}
