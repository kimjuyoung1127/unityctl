using System.Text.Json;
using Unityctl.Shared;
using Unityctl.Shared.Commands;
using Unityctl.Shared.Serialization;

namespace Unityctl.Cli.Commands;

/// <summary>
/// Machine-readable schema output for AI agents and tooling.
/// Outputs a JSON Schema-compatible document describing all available commands.
/// </summary>
public static class SchemaCommand
{
    public static void Execute(string format = "json")
    {
        var schema = BuildSchema();

        if (!string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Unknown format '{format}'. Supported formats: json");
            Environment.Exit(1);
            return;
        }

        Console.WriteLine(JsonSerializer.Serialize(schema, UnityctlJsonContext.Default.CommandSchema));
    }

    /// <summary>
    /// Builds the full CommandSchema from the catalog. Exposed for testing.
    /// </summary>
    internal static CommandSchema BuildSchema()
        => new()
        {
            Version = Constants.Version,
            Commands = CommandCatalog.All
        };
}
