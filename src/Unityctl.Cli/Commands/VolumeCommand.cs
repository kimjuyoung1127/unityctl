using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class VolumeCommand
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

    public static void SetOverride(string project, string id, string component, string property, string value, bool json = false)
    {
        var request = CreateSetOverrideRequest(id, component, property, value);
        CommandRunner.Execute(project, request, json);
    }

    public static void GetOverrides(string project, string id, string component, bool json = false)
    {
        var request = CreateGetOverridesRequest(id, component);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateListRequest(bool includeInactive)
    {
        var parameters = new JsonObject();
        if (includeInactive) parameters["includeInactive"] = true;

        return new CommandRequest
        {
            Command = WellKnownCommands.VolumeList,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetRequest(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        return new CommandRequest
        {
            Command = WellKnownCommands.VolumeGet,
            Parameters = new JsonObject { ["id"] = id }
        };
    }

    internal static CommandRequest CreateSetOverrideRequest(string id, string component, string property, string value)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(component))
            throw new ArgumentException("component must not be empty", nameof(component));
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("property must not be empty", nameof(property));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new CommandRequest
        {
            Command = WellKnownCommands.VolumeSetOverride,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["component"] = component,
                ["property"] = property,
                ["value"] = value
            }
        };
    }

    internal static CommandRequest CreateGetOverridesRequest(string id, string component)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(component))
            throw new ArgumentException("component must not be empty", nameof(component));

        return new CommandRequest
        {
            Command = WellKnownCommands.VolumeGetOverrides,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["component"] = component
            }
        };
    }
}
