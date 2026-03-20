using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class ModelCommandTests
{
    [Fact]
    public void GetImportSettingsRequest_HasCorrectCommand()
    {
        var request = ModelCommand.CreateGetImportSettingsRequest("Assets/Models/character.fbx");
        Assert.Equal(WellKnownCommands.ModelGetImportSettings, request.Command);
    }

    [Fact]
    public void GetImportSettingsRequest_SetsPath()
    {
        var request = ModelCommand.CreateGetImportSettingsRequest("Assets/Models/character.fbx");
        Assert.Equal("Assets/Models/character.fbx", request.Parameters!["path"]!.ToString());
    }

    [Fact]
    public void GetImportSettingsRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => ModelCommand.CreateGetImportSettingsRequest(""));
    }

    [Fact]
    public void GetImportSettingsRequest_NullPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => ModelCommand.CreateGetImportSettingsRequest(null!));
    }
}
