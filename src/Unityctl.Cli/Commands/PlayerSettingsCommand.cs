using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class PlayerSettingsCommand
{
    public static void Get(string project, string key, bool json = false)
    {
        var request = CreateGetRequest(key);
        CommandRunner.Execute(project, request, json);
    }

    public static void Set(string project, string key, string value, bool json = false)
    {
        var request = CreateSetRequest(key, value);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateGetRequest(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("key must not be empty", nameof(key));

        return new CommandRequest
        {
            Command = WellKnownCommands.PlayerSettings,
            Parameters = new JsonObject
            {
                ["action"] = "get",
                ["key"] = key
            }
        };
    }

    internal static CommandRequest CreateSetRequest(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("key must not be empty", nameof(key));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new CommandRequest
        {
            Command = WellKnownCommands.PlayerSettings,
            Parameters = new JsonObject
            {
                ["action"] = "set",
                ["key"] = key,
                ["value"] = value
            }
        };
    }
}
