using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class ProfilerCommand
{
    public static void GetStats(string project, bool json = false)
    {
        var request = CreateGetStatsRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void Start(string project, bool json = false)
    {
        var request = CreateStartRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void Stop(string project, bool json = false)
    {
        var request = CreateStopRequest();
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateGetStatsRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.ProfilerGetStats,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateStartRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.ProfilerStart,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateStopRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.ProfilerStop,
            Parameters = new JsonObject()
        };
    }
}
