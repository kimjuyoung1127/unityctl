using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class StatusTool(CommandExecutor executor)
{
    [McpServerTool(Name = "unityctl_status")]
    [Description("Check Unity editor and project compilation status")]
    public async Task<string> StatusAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("Retry until editor responds (default: false)")] bool wait = false,
        CancellationToken cancellationToken = default)
    {
        var request = new CommandRequest
        {
            Command = WellKnownCommands.Status,
            Parameters = new JsonObject { ["wait"] = wait }
        };
        var response = await executor.ExecuteAsync(project, request, retry: wait, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }
}
