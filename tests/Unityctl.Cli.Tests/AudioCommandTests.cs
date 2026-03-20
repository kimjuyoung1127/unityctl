using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class AudioCommandTests
{
    [Fact]
    public void GetImportSettingsRequest_HasCorrectCommand()
    {
        var request = AudioCommand.CreateGetImportSettingsRequest("Assets/Audio/bgm.wav");
        Assert.Equal(WellKnownCommands.AudioGetImportSettings, request.Command);
    }

    [Fact]
    public void GetImportSettingsRequest_SetsPath()
    {
        var request = AudioCommand.CreateGetImportSettingsRequest("Assets/Audio/bgm.wav");
        Assert.Equal("Assets/Audio/bgm.wav", request.Parameters!["path"]!.ToString());
    }

    [Fact]
    public void GetImportSettingsRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => AudioCommand.CreateGetImportSettingsRequest(""));
    }

    [Fact]
    public void GetImportSettingsRequest_NullPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => AudioCommand.CreateGetImportSettingsRequest(null!));
    }
}
