using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class AssetCommand
{
    public static void Refresh(string project, bool noWait = false, bool json = false)
    {
        var exitCode = ExecuteRefreshAsync(project, noWait, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> ExecuteRefreshAsync(string project, bool noWait, bool json)
    {
        var request = CreateRefreshRequest();
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        CommandResponse response;
        if (noWait)
        {
            response = await executor.ExecuteAsync(project, request);
        }
        else
        {
            response = await AsyncCommandRunner.ExecuteAsync(
                project,
                request,
                async (proj, req, ct) => await executor.ExecuteAsync(proj, req, ct: ct),
                pollCommand: WellKnownCommands.AssetRefreshResult,
                timeoutSeconds: 60,
                timeoutStatusCode: StatusCode.UnknownError,
                timeoutMessage: "Asset refresh timed out after 60s");
        }

        CommandRunner.PrintResponse(response, json);
        return CommandRunner.GetExitCode(response);
    }

    internal static CommandRequest CreateRefreshRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.AssetRefresh,
            Parameters = new JsonObject()
        };
    }
}
