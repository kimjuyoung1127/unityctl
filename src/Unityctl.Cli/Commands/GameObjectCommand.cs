using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class GameObjectCommand
{
    public static void Create(string project, string name, string? parent = null, string? scene = null, bool json = false)
    {
        var request = CreateCreateRequest(name, parent, scene);
        CommandRunner.Execute(project, request, json);
    }

    public static void Delete(string project, string id, bool json = false)
    {
        var request = CreateDeleteRequest(id);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetActive(string project, string id, string active, bool json = false)
    {
        var request = CreateSetActiveRequest(id, ParseActive(active));
        CommandRunner.Execute(project, request, json);
    }

    public static void Activate(string project, string id, bool json = false)
    {
        var request = CreateSetActiveRequest(id, true);
        CommandRunner.Execute(project, request, json);
    }

    public static void Deactivate(string project, string id, bool json = false)
    {
        var request = CreateSetActiveRequest(id, false);
        CommandRunner.Execute(project, request, json);
    }

    public static void Move(string project, string id, string parent, bool json = false)
    {
        var request = CreateMoveRequest(id, parent);
        CommandRunner.Execute(project, request, json);
    }

    public static void Rename(string project, string id, string name, bool json = false)
    {
        var request = CreateRenameRequest(id, name);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateCreateRequest(string name, string? parent, string? scene)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));

        var parameters = new JsonObject { ["name"] = name };
        if (!string.IsNullOrEmpty(parent)) parameters["parent"] = parent;
        if (!string.IsNullOrEmpty(scene)) parameters["scene"] = scene;

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectCreate,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateDeleteRequest(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectDelete,
            Parameters = new JsonObject { ["id"] = id }
        };
    }

    internal static CommandRequest CreateSetActiveRequest(string id, bool active)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectSetActive,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["active"] = active
            }
        };
    }

    internal static bool ParseActive(string active)
    {
        if (string.IsNullOrWhiteSpace(active))
            throw new ArgumentException("active must not be empty", nameof(active));

        if (bool.TryParse(active, out var parsed))
            return parsed;

        switch (active.Trim().ToLowerInvariant())
        {
            case "1":
            case "on":
            case "enable":
            case "enabled":
            case "active":
                return true;
            case "0":
            case "off":
            case "disable":
            case "disabled":
            case "inactive":
                return false;
        }

        throw new ArgumentException("active must be 'true' or 'false'", nameof(active));
    }

    internal static CommandRequest CreateMoveRequest(string id, string parent)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(parent))
            throw new ArgumentException("parent must not be empty", nameof(parent));

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectMove,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["parent"] = parent
            }
        };
    }

    internal static CommandRequest CreateRenameRequest(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectRename,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["name"] = name
            }
        };
    }
}
