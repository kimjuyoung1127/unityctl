using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class BuildCommand
{
    public static void Execute(string project, string target = "StandaloneWindows64", string? output = null, bool dryRun = false, bool json = false)
    {
        var request = CreateRequest(target, output, dryRun);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateRequest(string target, string? output, bool dryRun)
    {
        var parameters = new JsonObject
        {
            ["target"] = target,
            ["outputPath"] = output
        };

        if (dryRun) parameters["dryRun"] = true;

        return new CommandRequest
        {
            Command = WellKnownCommands.Build,
            Parameters = parameters
        };
    }
}
