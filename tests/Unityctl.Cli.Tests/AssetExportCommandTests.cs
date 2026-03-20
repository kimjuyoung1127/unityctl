using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class AssetExportCommandTests
{
    [Fact]
    public void ExportRequest_HasCorrectCommand()
    {
        var request = AssetCommand.CreateExportRequest("Assets/Prefabs/Player.prefab", "C:/temp/export.unitypackage", true);
        Assert.Equal(WellKnownCommands.AssetExport, request.Command);
    }

    [Fact]
    public void ExportRequest_SetsAllParameters()
    {
        var request = AssetCommand.CreateExportRequest("Assets/Prefabs/Player.prefab", "C:/temp/export.unitypackage", false);
        Assert.Equal("Assets/Prefabs/Player.prefab", request.Parameters!["paths"]!.ToString());
        Assert.Equal("C:/temp/export.unitypackage", request.Parameters!["output"]!.ToString());
        Assert.False((bool)request.Parameters!["includeDependencies"]!);
    }

    [Fact]
    public void ExportRequest_EmptyPaths_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AssetCommand.CreateExportRequest("", "C:/temp/export.unitypackage", true));
    }

    [Fact]
    public void ExportRequest_EmptyOutput_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AssetCommand.CreateExportRequest("Assets/Prefabs/Player.prefab", "", true));
    }
}
