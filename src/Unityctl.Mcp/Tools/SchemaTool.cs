using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Unityctl.Shared.Commands;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class SchemaTool
{
    [McpServerTool(Name = "unityctl_schema")]
    [Description("Return the machine-readable JSON schema of all available unityctl commands (tool discovery)")]
    public string Schema()
    {
        var schema = new CommandSchema
        {
            Version = Unityctl.Shared.Constants.Version,
            Commands = CommandCatalog.All
        };
        return JsonSerializer.Serialize(schema, UnityctlJsonContext.Default.CommandSchema);
    }
}
