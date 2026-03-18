using System.Text.Json.Nodes;
using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class BuildCommandDryRunTests
{
    [Fact]
    public void DryRun_True_AddsParameterToRequest()
    {
        var request = BuildCommand.CreateRequest("StandaloneWindows64", null, dryRun: true);

        Assert.NotNull(request.Parameters);
        Assert.True(request.Parameters!["dryRun"]?.GetValue<bool>());
    }

    [Fact]
    public void DryRun_False_OmitsParameter()
    {
        var request = BuildCommand.CreateRequest("StandaloneWindows64", null, dryRun: false);

        Assert.NotNull(request.Parameters);
        Assert.Null(request.Parameters!["dryRun"]);
    }

    [Fact]
    public void CreateRequest_SetsCorrectCommand()
    {
        var request = BuildCommand.CreateRequest("Android", "/output/path", dryRun: false);

        Assert.Equal(WellKnownCommands.Build, request.Command);
    }

    [Fact]
    public void CreateRequest_SetsTargetAndOutputPath()
    {
        var request = BuildCommand.CreateRequest("Android", "/output/build", dryRun: false);

        Assert.Equal("Android", request.Parameters!["target"]?.GetValue<string>());
        Assert.Equal("/output/build", request.Parameters["outputPath"]?.GetValue<string>());
    }

    [Fact]
    public void CreateRequest_NullOutput_IsAllowed()
    {
        var request = BuildCommand.CreateRequest("WebGL", null, dryRun: true);

        Assert.NotNull(request);
        Assert.Equal("WebGL", request.Parameters!["target"]?.GetValue<string>());
        Assert.True(request.Parameters["dryRun"]?.GetValue<bool>());
    }

    [Fact]
    public void CreateRequest_DryRun_DoesNotAffectTargetOrOutput()
    {
        var withDryRun = BuildCommand.CreateRequest("iOS", "/out", dryRun: true);
        var withoutDryRun = BuildCommand.CreateRequest("iOS", "/out", dryRun: false);

        Assert.Equal(
            withoutDryRun.Parameters!["target"]?.GetValue<string>(),
            withDryRun.Parameters!["target"]?.GetValue<string>());
        Assert.Equal(
            withoutDryRun.Parameters["outputPath"]?.GetValue<string>(),
            withDryRun.Parameters["outputPath"]?.GetValue<string>());
    }
}
