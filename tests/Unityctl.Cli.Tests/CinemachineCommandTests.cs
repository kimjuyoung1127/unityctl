using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class CinemachineCommandTests
{
    [Fact]
    public void ListRequest_HasCorrectCommand()
    {
        var request = CinemachineCommand.CreateListRequest(false);
        Assert.Equal(WellKnownCommands.CinemachineList, request.Command);
    }

    [Fact]
    public void ListRequest_IncludeInactive_SetsParameter()
    {
        var request = CinemachineCommand.CreateListRequest(true);
        Assert.True((bool)request.Parameters!["includeInactive"]!);
    }

    [Fact]
    public void GetRequest_HasCorrectCommand()
    {
        var request = CinemachineCommand.CreateGetRequest("goid123");
        Assert.Equal(WellKnownCommands.CinemachineGet, request.Command);
    }

    [Fact]
    public void GetRequest_SetsId()
    {
        var request = CinemachineCommand.CreateGetRequest("goid123");
        Assert.Equal("goid123", request.Parameters!["id"]!.ToString());
    }

    [Fact]
    public void GetRequest_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CinemachineCommand.CreateGetRequest(""));
    }

    [Fact]
    public void SetPropertyRequest_HasCorrectCommand()
    {
        var request = CinemachineCommand.CreateSetPropertyRequest("goid123", "m_Lens.FieldOfView", "60");
        Assert.Equal(WellKnownCommands.CinemachineSetProperty, request.Command);
    }

    [Fact]
    public void SetPropertyRequest_SetsAllParameters()
    {
        var request = CinemachineCommand.CreateSetPropertyRequest("goid123", "Priority", "10");
        Assert.Equal("goid123", request.Parameters!["id"]!.ToString());
        Assert.Equal("Priority", request.Parameters!["property"]!.ToString());
        Assert.Equal("10", request.Parameters!["value"]!.ToString());
    }

    [Fact]
    public void SetPropertyRequest_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CinemachineCommand.CreateSetPropertyRequest("", "Priority", "10"));
    }

    [Fact]
    public void SetPropertyRequest_EmptyProperty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CinemachineCommand.CreateSetPropertyRequest("goid123", "", "10"));
    }

    [Fact]
    public void SetPropertyRequest_NullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CinemachineCommand.CreateSetPropertyRequest("goid123", "Priority", null!));
    }
}
