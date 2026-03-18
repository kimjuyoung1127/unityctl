using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class SceneTool(CommandExecutor executor)
{
    [McpServerTool(Name = "unityctl_scene_snapshot")]
    [Description("Capture a snapshot of all scene objects and their serialized properties")]
    public async Task<string> SceneSnapshotAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("Filter to a specific scene path (optional)")] string? scenePath = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrEmpty(scenePath)) parameters["scenePath"] = scenePath;

        var request = new CommandRequest
        {
            Command = WellKnownCommands.SceneSnapshot,
            Parameters = parameters
        };
        var response = await executor.ExecuteAsync(project, request, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }

    [McpServerTool(Name = "unityctl_scene_diff")]
    [Description("Compare the current scene against the last captured snapshot to report property-level changes")]
    public async Task<string> SceneDiffAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("Float comparison threshold (default: 1e-6)")] double epsilon = 1e-6,
        CancellationToken cancellationToken = default)
    {
        var request = new CommandRequest
        {
            Command = WellKnownCommands.SceneDiff,
            Parameters = new JsonObject
            {
                ["live"] = true,
                ["epsilon"] = epsilon
            }
        };
        var response = await executor.ExecuteAsync(project, request, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }
}
