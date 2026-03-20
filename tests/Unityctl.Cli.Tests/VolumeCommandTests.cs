using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class VolumeCommandTests
{
    [Fact]
    public void ListRequest_HasCorrectCommand()
    {
        var request = VolumeCommand.CreateListRequest(false);
        Assert.Equal(WellKnownCommands.VolumeList, request.Command);
    }

    [Fact]
    public void ListRequest_IncludeInactive_SetsParameter()
    {
        var request = VolumeCommand.CreateListRequest(true);
        Assert.True((bool)request.Parameters!["includeInactive"]!);
    }

    [Fact]
    public void GetRequest_HasCorrectCommand()
    {
        var request = VolumeCommand.CreateGetRequest("goid123");
        Assert.Equal(WellKnownCommands.VolumeGet, request.Command);
    }

    [Fact]
    public void GetRequest_SetsId()
    {
        var request = VolumeCommand.CreateGetRequest("goid123");
        Assert.Equal("goid123", request.Parameters!["id"]!.ToString());
    }

    [Fact]
    public void GetRequest_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => VolumeCommand.CreateGetRequest(""));
    }

    [Fact]
    public void SetOverrideRequest_HasCorrectCommand()
    {
        var request = VolumeCommand.CreateSetOverrideRequest("goid123", "Bloom", "intensity", "0.5");
        Assert.Equal(WellKnownCommands.VolumeSetOverride, request.Command);
    }

    [Fact]
    public void SetOverrideRequest_SetsAllParameters()
    {
        var request = VolumeCommand.CreateSetOverrideRequest("goid123", "Bloom", "intensity", "0.5");
        Assert.Equal("goid123", request.Parameters!["id"]!.ToString());
        Assert.Equal("Bloom", request.Parameters!["component"]!.ToString());
        Assert.Equal("intensity", request.Parameters!["property"]!.ToString());
        Assert.Equal("0.5", request.Parameters!["value"]!.ToString());
    }

    [Fact]
    public void SetOverrideRequest_EmptyComponent_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            VolumeCommand.CreateSetOverrideRequest("goid123", "", "intensity", "0.5"));
    }

    [Fact]
    public void SetOverrideRequest_EmptyProperty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            VolumeCommand.CreateSetOverrideRequest("goid123", "Bloom", "", "0.5"));
    }

    [Fact]
    public void GetOverridesRequest_HasCorrectCommand()
    {
        var request = VolumeCommand.CreateGetOverridesRequest("goid123", "Bloom");
        Assert.Equal(WellKnownCommands.VolumeGetOverrides, request.Command);
    }

    [Fact]
    public void GetOverridesRequest_SetsParameters()
    {
        var request = VolumeCommand.CreateGetOverridesRequest("goid123", "Vignette");
        Assert.Equal("goid123", request.Parameters!["id"]!.ToString());
        Assert.Equal("Vignette", request.Parameters!["component"]!.ToString());
    }

    [Fact]
    public void GetOverridesRequest_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => VolumeCommand.CreateGetOverridesRequest("", "Bloom"));
    }

    [Fact]
    public void GetOverridesRequest_EmptyComponent_Throws()
    {
        Assert.Throws<ArgumentException>(() => VolumeCommand.CreateGetOverridesRequest("goid123", ""));
    }
}

public class RendererFeatureCommandTests
{
    [Fact]
    public void ListRequest_HasCorrectCommand()
    {
        var request = RendererFeatureCommand.CreateListRequest();
        Assert.Equal(WellKnownCommands.RendererFeatureList, request.Command);
    }

    [Fact]
    public void ListRequest_HasEmptyParameters()
    {
        var request = RendererFeatureCommand.CreateListRequest();
        Assert.NotNull(request.Parameters);
    }
}
