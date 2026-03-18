using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class PingTool(CommandExecutor executor)
{
    [McpServerTool(Name = "unityctl_ping")]
    [Description("Verify unityctl connectivity to a running Unity Editor")]
    public async Task<string> PingAsync(
        [Description("Path to the Unity project directory")] string project,
        CancellationToken cancellationToken)
    {
        var request = new CommandRequest { Command = WellKnownCommands.Ping };
        var response = await executor.ExecuteAsync(project, request, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }
}
