using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class AudioCommand
{
    public static void GetImportSettings(string project, string path, bool json = false)
    {
        var request = CreateGetImportSettingsRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateGetImportSettingsRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AudioGetImportSettings,
            Parameters = new JsonObject { ["path"] = path }
        };
    }
}
