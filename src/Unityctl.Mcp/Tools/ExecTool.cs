using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class ExecTool(CommandExecutor executor)
{
    [McpServerTool(Name = "unityctl_exec")]
    [Description("Execute a C# expression in the Unity Editor via reflection (trust the agent)")]
    public async Task<string> ExecAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("C# expression to evaluate (e.g. 'EditorApplication.isPlaying = true')")] string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return JsonSerializer.Serialize(
                CommandResponse.Fail(StatusCode.InvalidParameters, "code must not be empty"),
                UnityctlJsonContext.Default.CommandResponse);

        var request = new CommandRequest
        {
            Command = WellKnownCommands.Exec,
            Parameters = new JsonObject { ["code"] = code }
        };
        var response = await executor.ExecuteAsync(project, request, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }
}
