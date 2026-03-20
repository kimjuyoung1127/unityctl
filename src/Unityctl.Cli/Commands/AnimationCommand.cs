using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class AnimationCommand
{
    public static void CreateClip(string project, string path, bool json = false)
    {
        var request = CreateCreateClipRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void CreateController(string project, string path, bool json = false)
    {
        var request = CreateCreateControllerRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateCreateClipRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AnimationCreateClip,
            Parameters = new JsonObject { ["path"] = path }
        };
    }

    internal static CommandRequest CreateCreateControllerRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AnimationCreateController,
            Parameters = new JsonObject { ["path"] = path }
        };
    }

    // Phase H: Animation Workflow Extension
    public static void ListClips(string project, string? folder = null, string? filter = null, int? limit = null, bool json = false)
    {
        var request = CreateListClipsRequest(folder, filter, limit);
        CommandRunner.Execute(project, request, json);
    }

    public static void GetClip(string project, string path, bool json = false)
    {
        var request = CreateGetClipRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void GetController(string project, string path, bool json = false)
    {
        var request = CreateGetControllerRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void AddCurve(string project, string path, string binding, string keys, bool json = false)
    {
        var request = CreateAddCurveRequest(path, binding, keys);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateListClipsRequest(string? folder, string? filter, int? limit)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(folder))
            parameters["folder"] = folder;
        if (!string.IsNullOrWhiteSpace(filter))
            parameters["filter"] = filter;
        if (limit.HasValue)
            parameters["limit"] = limit.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.AnimationListClips,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetClipRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AnimationGetClip,
            Parameters = new JsonObject { ["path"] = path }
        };
    }

    internal static CommandRequest CreateGetControllerRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AnimationGetController,
            Parameters = new JsonObject { ["path"] = path }
        };
    }

    internal static CommandRequest CreateAddCurveRequest(string path, string binding, string keys)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (string.IsNullOrWhiteSpace(binding))
            throw new ArgumentException("binding must not be empty", nameof(binding));
        if (string.IsNullOrWhiteSpace(keys))
            throw new ArgumentException("keys must not be empty", nameof(keys));

        return new CommandRequest
        {
            Command = WellKnownCommands.AnimationAddCurve,
            Parameters = new JsonObject
            {
                ["path"] = path,
                ["binding"] = binding,
                ["keys"] = keys
            }
        };
    }
}
