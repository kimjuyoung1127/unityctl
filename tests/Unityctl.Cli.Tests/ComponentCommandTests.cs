using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class ComponentCommandTests
{
    // === Add ===

    [CliTestFact]
    public void Add_SetsCommandName()
    {
        var request = ComponentCommand.CreateAddRequest("gid-go", "UnityEngine.Rigidbody");
        Assert.Equal(WellKnownCommands.ComponentAdd, request.Command);
    }

    [CliTestFact]
    public void Add_SetsIdAndType()
    {
        var request = ComponentCommand.CreateAddRequest("gid-go", "UnityEngine.BoxCollider");
        Assert.Equal("gid-go", request.Parameters!["id"]?.GetValue<string>());
        Assert.Equal("UnityEngine.BoxCollider", request.Parameters!["type"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Add_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateAddRequest("", "Rigidbody"));
    }

    [CliTestFact]
    public void Add_EmptyType_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateAddRequest("gid", ""));
    }

    [CliTestFact]
    public void Add_HasRequestId()
    {
        var request = ComponentCommand.CreateAddRequest("gid", "Rigidbody");
        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }

    // === Remove ===

    [CliTestFact]
    public void Remove_SetsCommandName()
    {
        var request = ComponentCommand.CreateRemoveRequest("comp-gid");
        Assert.Equal(WellKnownCommands.ComponentRemove, request.Command);
    }

    [CliTestFact]
    public void Remove_SetsComponentId()
    {
        var request = ComponentCommand.CreateRemoveRequest("comp-gid-123");
        Assert.Equal("comp-gid-123", request.Parameters!["componentId"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Remove_EmptyComponentId_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateRemoveRequest(""));
    }

    // === SetProperty ===

    [CliTestFact]
    public void SetProperty_SetsCommandName()
    {
        var request = ComponentCommand.CreateSetPropertyRequest("comp-gid", "mass", "5");
        Assert.Equal(WellKnownCommands.ComponentSetProperty, request.Command);
    }

    [CliTestFact]
    public void SetProperty_SetsAllParameters()
    {
        var request = ComponentCommand.CreateSetPropertyRequest("comp-gid", "m_LocalPosition", "{\"x\":1,\"y\":2,\"z\":3}");
        Assert.Equal("comp-gid", request.Parameters!["componentId"]?.GetValue<string>());
        Assert.Equal("m_LocalPosition", request.Parameters!["property"]?.GetValue<string>());
        Assert.Equal("{\"x\":1,\"y\":2,\"z\":3}", request.Parameters!["value"]?.GetValue<string>());
    }

    [CliTestFact]
    public void SetProperty_EmptyComponentId_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateSetPropertyRequest("", "prop", "val"));
    }

    [CliTestFact]
    public void SetProperty_EmptyProperty_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateSetPropertyRequest("gid", "", "val"));
    }

    [CliTestFact]
    public void SetProperty_NullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ComponentCommand.CreateSetPropertyRequest("gid", "prop", null!));
    }

    [CliTestFact]
    public void SetProperty_HasRequestId()
    {
        var request = ComponentCommand.CreateSetPropertyRequest("gid", "prop", "val");
        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }
}
