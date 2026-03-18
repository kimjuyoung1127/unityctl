using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class PlayModeCommand
{
    public static void Execute(string project, string action, bool json = false)
    {
        var request = CreateRequest(action);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateRequest(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("action must not be empty", nameof(action));

        return new CommandRequest
        {
            Command = WellKnownCommands.PlayMode,
            Parameters = new JsonObject { ["action"] = action }
        };
    }
}
