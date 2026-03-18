using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class GameObjectCommandTests
{
    // === Create ===

    [CliTestFact]
    public void Create_SetsCommandName()
    {
        var request = GameObjectCommand.CreateCreateRequest("Cube", null, null);
        Assert.Equal(WellKnownCommands.GameObjectCreate, request.Command);
    }

    [CliTestFact]
    public void Create_SetsNameParameter()
    {
        var request = GameObjectCommand.CreateCreateRequest("TestObj", null, null);
        Assert.Equal("TestObj", request.Parameters!["name"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Create_SetsParentWhenProvided()
    {
        var request = GameObjectCommand.CreateCreateRequest("Child", "GlobalObjectId_V1-2-abc", null);
        Assert.Equal("GlobalObjectId_V1-2-abc", request.Parameters!["parent"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Create_SetsSceneWhenProvided()
    {
        var request = GameObjectCommand.CreateCreateRequest("Obj", null, "Assets/Scenes/Main.unity");
        Assert.Equal("Assets/Scenes/Main.unity", request.Parameters!["scene"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Create_OmitsParentAndSceneWhenNull()
    {
        var request = GameObjectCommand.CreateCreateRequest("Obj", null, null);
        Assert.False(request.Parameters!.ContainsKey("parent"));
        Assert.False(request.Parameters!.ContainsKey("scene"));
    }

    [CliTestFact]
    public void Create_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateCreateRequest("", null, null));
    }

    // === Delete ===

    [CliTestFact]
    public void Delete_SetsCommandName()
    {
        var request = GameObjectCommand.CreateDeleteRequest("GlobalObjectId_V1-2-xyz");
        Assert.Equal(WellKnownCommands.GameObjectDelete, request.Command);
    }

    [CliTestFact]
    public void Delete_SetsIdParameter()
    {
        var request = GameObjectCommand.CreateDeleteRequest("gid-123");
        Assert.Equal("gid-123", request.Parameters!["id"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Delete_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateDeleteRequest(""));
    }

    // === SetActive ===

    [CliTestFact]
    public void SetActive_SetsCommandName()
    {
        var request = GameObjectCommand.CreateSetActiveRequest("gid", true);
        Assert.Equal(WellKnownCommands.GameObjectSetActive, request.Command);
    }

    [CliTestFact]
    public void SetActive_SetsActiveTrue()
    {
        var request = GameObjectCommand.CreateSetActiveRequest("gid", true);
        Assert.True(request.Parameters!["active"]?.GetValue<bool>());
    }

    [CliTestFact]
    public void SetActive_SetsActiveFalse()
    {
        var request = GameObjectCommand.CreateSetActiveRequest("gid", false);
        Assert.False(request.Parameters!["active"]?.GetValue<bool>());
    }

    [CliTestFact]
    public void SetActive_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateSetActiveRequest("", true));
    }

    [CliTestFact]
    public void SetActive_ParseActive_ParsesTrue()
    {
        Assert.True(GameObjectCommand.ParseActive("true"));
    }

    [CliTestFact]
    public void SetActive_ParseActive_ParsesFalse()
    {
        Assert.False(GameObjectCommand.ParseActive("false"));
    }

    [CliTestFact]
    public void SetActive_ParseActive_Invalid_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.ParseActive("nope"));
    }

    [CliTestFact]
    public void SetActive_ParseActive_ParsesOn()
    {
        Assert.True(GameObjectCommand.ParseActive("on"));
    }

    [CliTestFact]
    public void SetActive_ParseActive_ParsesOff()
    {
        Assert.False(GameObjectCommand.ParseActive("off"));
    }

    // === Move ===

    [CliTestFact]
    public void Move_SetsCommandName()
    {
        var request = GameObjectCommand.CreateMoveRequest("child-gid", "parent-gid");
        Assert.Equal(WellKnownCommands.GameObjectMove, request.Command);
    }

    [CliTestFact]
    public void Move_SetsIdAndParent()
    {
        var request = GameObjectCommand.CreateMoveRequest("child-gid", "parent-gid");
        Assert.Equal("child-gid", request.Parameters!["id"]?.GetValue<string>());
        Assert.Equal("parent-gid", request.Parameters!["parent"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Move_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateMoveRequest("", "p"));
    }

    [CliTestFact]
    public void Move_EmptyParent_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateMoveRequest("c", ""));
    }

    // === Rename ===

    [CliTestFact]
    public void Rename_SetsCommandName()
    {
        var request = GameObjectCommand.CreateRenameRequest("gid", "NewName");
        Assert.Equal(WellKnownCommands.GameObjectRename, request.Command);
    }

    [CliTestFact]
    public void Rename_SetsIdAndName()
    {
        var request = GameObjectCommand.CreateRenameRequest("gid", "NewName");
        Assert.Equal("gid", request.Parameters!["id"]?.GetValue<string>());
        Assert.Equal("NewName", request.Parameters!["name"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Rename_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateRenameRequest("", "name"));
    }

    [CliTestFact]
    public void Rename_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateRenameRequest("gid", ""));
    }

    // === SceneSave ===

    [CliTestFact]
    public void SceneSave_SetsCommandName()
    {
        var request = SceneCommand.CreateSaveRequest(null, false);
        Assert.Equal(WellKnownCommands.SceneSave, request.Command);
    }

    [CliTestFact]
    public void SceneSave_SetsSceneWhenProvided()
    {
        var request = SceneCommand.CreateSaveRequest("Assets/Scenes/Main.unity", false);
        Assert.Equal("Assets/Scenes/Main.unity", request.Parameters!["scene"]?.GetValue<string>());
    }

    [CliTestFact]
    public void SceneSave_SetsAllWhenTrue()
    {
        var request = SceneCommand.CreateSaveRequest(null, true);
        Assert.True(request.Parameters!["all"]?.GetValue<bool>());
    }

    [CliTestFact]
    public void SceneSave_OmitsSceneAndAllWhenDefaults()
    {
        var request = SceneCommand.CreateSaveRequest(null, false);
        Assert.False(request.Parameters!.ContainsKey("scene"));
        Assert.False(request.Parameters!.ContainsKey("all"));
    }

    // === HasRequestId ===

    [CliTestFact]
    public void AllRequests_HaveRequestId()
    {
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateCreateRequest("x", null, null).RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateDeleteRequest("x").RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateSetActiveRequest("x", true).RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateMoveRequest("x", "y").RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateRenameRequest("x", "y").RequestId));
        Assert.False(string.IsNullOrEmpty(SceneCommand.CreateSaveRequest(null, false).RequestId));
    }
}
