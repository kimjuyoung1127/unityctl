using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class AnimationExtensionCommandTests
{
    // ListClips
    [Fact]
    public void ListClipsRequest_HasCorrectCommand()
    {
        var request = AnimationCommand.CreateListClipsRequest(null, null, null);
        Assert.Equal(WellKnownCommands.AnimationListClips, request.Command);
    }

    [Fact]
    public void ListClipsRequest_SetsOptionalParameters()
    {
        var request = AnimationCommand.CreateListClipsRequest("Assets/Animations", "walk", 10);
        Assert.Equal("Assets/Animations", request.Parameters!["folder"]!.ToString());
        Assert.Equal("walk", request.Parameters!["filter"]!.ToString());
        Assert.Equal(10, (int)request.Parameters!["limit"]!);
    }

    [Fact]
    public void ListClipsRequest_OmitsNullParameters()
    {
        var request = AnimationCommand.CreateListClipsRequest(null, null, null);
        Assert.Null(request.Parameters!["folder"]);
        Assert.Null(request.Parameters!["filter"]);
        Assert.Null(request.Parameters!["limit"]);
    }

    // GetClip
    [Fact]
    public void GetClipRequest_HasCorrectCommand()
    {
        var request = AnimationCommand.CreateGetClipRequest("Assets/Animations/walk.anim");
        Assert.Equal(WellKnownCommands.AnimationGetClip, request.Command);
    }

    [Fact]
    public void GetClipRequest_SetsPath()
    {
        var request = AnimationCommand.CreateGetClipRequest("Assets/Animations/walk.anim");
        Assert.Equal("Assets/Animations/walk.anim", request.Parameters!["path"]!.ToString());
    }

    [Fact]
    public void GetClipRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => AnimationCommand.CreateGetClipRequest(""));
    }

    // GetController
    [Fact]
    public void GetControllerRequest_HasCorrectCommand()
    {
        var request = AnimationCommand.CreateGetControllerRequest("Assets/Animations/PlayerController.controller");
        Assert.Equal(WellKnownCommands.AnimationGetController, request.Command);
    }

    [Fact]
    public void GetControllerRequest_SetsPath()
    {
        var request = AnimationCommand.CreateGetControllerRequest("Assets/Animations/PlayerController.controller");
        Assert.Equal("Assets/Animations/PlayerController.controller", request.Parameters!["path"]!.ToString());
    }

    [Fact]
    public void GetControllerRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => AnimationCommand.CreateGetControllerRequest(""));
    }

    // AddCurve
    [Fact]
    public void AddCurveRequest_HasCorrectCommand()
    {
        var request = AnimationCommand.CreateAddCurveRequest(
            "Assets/Animations/walk.anim",
            "{\"path\":\"\",\"type\":\"UnityEngine.Transform\",\"propertyName\":\"m_LocalPosition.x\"}",
            "[{\"time\":0,\"value\":0},{\"time\":1,\"value\":1}]");
        Assert.Equal(WellKnownCommands.AnimationAddCurve, request.Command);
    }

    [Fact]
    public void AddCurveRequest_SetsAllParameters()
    {
        var binding = "{\"path\":\"\",\"type\":\"Transform\",\"propertyName\":\"m_LocalPosition.x\"}";
        var keys = "[{\"time\":0,\"value\":0}]";
        var request = AnimationCommand.CreateAddCurveRequest("Assets/Animations/walk.anim", binding, keys);
        Assert.Equal("Assets/Animations/walk.anim", request.Parameters!["path"]!.ToString());
        Assert.Equal(binding, request.Parameters!["binding"]!.ToString());
        Assert.Equal(keys, request.Parameters!["keys"]!.ToString());
    }

    [Fact]
    public void AddCurveRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AnimationCommand.CreateAddCurveRequest("", "{}", "[]"));
    }

    [Fact]
    public void AddCurveRequest_EmptyBinding_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AnimationCommand.CreateAddCurveRequest("Assets/a.anim", "", "[]"));
    }

    [Fact]
    public void AddCurveRequest_EmptyKeys_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AnimationCommand.CreateAddCurveRequest("Assets/a.anim", "{}", ""));
    }
}
