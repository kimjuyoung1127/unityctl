using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class CinemachineCommand
{
    public static void List(string project, bool includeInactive = false, bool json = false)
    {
        var request = CreateListRequest(includeInactive);
        CommandRunner.Execute(project, request, json);
    }

    public static void Get(string project, string id, bool json = false)
    {
        var request = CreateGetRequest(id);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetProperty(string project, string id, string property, string value, bool json = false)
    {
        var request = CreateSetPropertyRequest(id, property, value);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateListRequest(bool includeInactive)
    {
        var parameters = new JsonObject();
        if (includeInactive) parameters["includeInactive"] = true;

        return new CommandRequest
        {
            Command = WellKnownCommands.CinemachineList,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetRequest(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        return new CommandRequest
        {
            Command = WellKnownCommands.CinemachineGet,
            Parameters = new JsonObject { ["id"] = id }
        };
    }

    internal static CommandRequest CreateSetPropertyRequest(string id, string property, string value)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("property must not be empty", nameof(property));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new CommandRequest
        {
            Command = WellKnownCommands.CinemachineSetProperty,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["property"] = property,
                ["value"] = value
            }
        };
    }
}
