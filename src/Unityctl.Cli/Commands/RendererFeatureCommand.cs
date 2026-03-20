using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class RendererFeatureCommand
{
    public static void List(string project, bool json = false)
    {
        var request = CreateListRequest();
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateListRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.RendererFeatureList,
            Parameters = new JsonObject()
        };
    }
}
