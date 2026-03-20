using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class UitkCommand
{
    public static void Find(string project, string? name = null, string? className = null, string? type = null, int? limit = null, bool json = false)
    {
        var request = CreateFindRequest(name, className, type, limit);
        CommandRunner.Execute(project, request, json);
    }

    public static void Get(string project, string name, bool json = false)
    {
        var request = CreateGetRequest(name);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetValue(string project, string name, string value, bool json = false)
    {
        var request = CreateSetValueRequest(name, value);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateFindRequest(string? name, string? className, string? type, int? limit)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(name)) parameters["name"] = name;
        if (!string.IsNullOrWhiteSpace(className)) parameters["className"] = className;
        if (!string.IsNullOrWhiteSpace(type)) parameters["type"] = type;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.UitkFind,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetRequest(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));

        return new CommandRequest
        {
            Command = WellKnownCommands.UitkGet,
            Parameters = new JsonObject { ["name"] = name }
        };
    }

    internal static CommandRequest CreateSetValueRequest(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new CommandRequest
        {
            Command = WellKnownCommands.UitkSetValue,
            Parameters = new JsonObject
            {
                ["name"] = name,
                ["value"] = value
            }
        };
    }
}
