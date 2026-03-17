using System.Text.Json.Nodes;
using Unityctl.Cli.Output;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class CheckCommand
{
    public static void Execute(string project, string type = "compile", bool json = false)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        var request = new CommandRequest
        {
            Command = "check",
            Parameters = new JsonObject
            {
                ["type"] = type
            }
        };

        var response = executor.ExecuteAsync(project, request).GetAwaiter().GetResult();

        if (json)
            JsonOutput.PrintResponse(response);
        else
        {
            ConsoleOutput.PrintResponse(response);
            if (!response.Success)
                ConsoleOutput.PrintRecovery(response.StatusCode);
        }

        Environment.Exit(response.Success ? 0 : 1);
    }
}
