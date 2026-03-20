using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class ProfilerCommandTests
{
    [Fact]
    public void GetStatsRequest_HasCorrectCommand()
    {
        var request = ProfilerCommand.CreateGetStatsRequest();
        Assert.Equal(WellKnownCommands.ProfilerGetStats, request.Command);
    }

    [Fact]
    public void GetStatsRequest_HasEmptyParameters()
    {
        var request = ProfilerCommand.CreateGetStatsRequest();
        Assert.NotNull(request.Parameters);
    }

    [Fact]
    public void StartRequest_HasCorrectCommand()
    {
        var request = ProfilerCommand.CreateStartRequest();
        Assert.Equal(WellKnownCommands.ProfilerStart, request.Command);
    }

    [Fact]
    public void StartRequest_HasEmptyParameters()
    {
        var request = ProfilerCommand.CreateStartRequest();
        Assert.NotNull(request.Parameters);
    }

    [Fact]
    public void StopRequest_HasCorrectCommand()
    {
        var request = ProfilerCommand.CreateStopRequest();
        Assert.Equal(WellKnownCommands.ProfilerStop, request.Command);
    }

    [Fact]
    public void StopRequest_HasEmptyParameters()
    {
        var request = ProfilerCommand.CreateStopRequest();
        Assert.NotNull(request.Parameters);
    }
}
