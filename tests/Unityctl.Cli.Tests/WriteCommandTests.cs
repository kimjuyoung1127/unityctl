using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class WriteCommandTests
{
    // === PlayModeCommand ===

    [CliTestFact]
    public void PlayMode_CreateRequest_SetsCommandName()
    {
        var request = PlayModeCommand.CreateRequest("start");
        Assert.Equal(WellKnownCommands.PlayMode, request.Command);
    }

    [CliTestFact]
    public void PlayMode_CreateRequest_SetsActionParameter()
    {
        var request = PlayModeCommand.CreateRequest("stop");
        Assert.Equal("stop", request.Parameters!["action"]?.GetValue<string>());
    }

    [CliTestFact]
    public void PlayMode_CreateRequest_EmptyAction_Throws()
    {
        Assert.Throws<ArgumentException>(() => PlayModeCommand.CreateRequest(""));
    }

    [CliTestFact]
    public void PlayMode_CreateRequest_HasRequestId()
    {
        var request = PlayModeCommand.CreateRequest("pause");
        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }

    // === PlayerSettingsCommand ===

    [CliTestFact]
    public void PlayerSettings_CreateGetRequest_SetsCommandName()
    {
        var request = PlayerSettingsCommand.CreateGetRequest("companyName");
        Assert.Equal(WellKnownCommands.PlayerSettings, request.Command);
    }

    [CliTestFact]
    public void PlayerSettings_CreateGetRequest_SetsActionToGet()
    {
        var request = PlayerSettingsCommand.CreateGetRequest("companyName");
        Assert.Equal("get", request.Parameters!["action"]?.GetValue<string>());
    }

    [CliTestFact]
    public void PlayerSettings_CreateGetRequest_SetsKey()
    {
        var request = PlayerSettingsCommand.CreateGetRequest("productName");
        Assert.Equal("productName", request.Parameters!["key"]?.GetValue<string>());
    }

    [CliTestFact]
    public void PlayerSettings_CreateGetRequest_EmptyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => PlayerSettingsCommand.CreateGetRequest(""));
    }

    [CliTestFact]
    public void PlayerSettings_CreateSetRequest_SetsCommandName()
    {
        var request = PlayerSettingsCommand.CreateSetRequest("companyName", "TestCo");
        Assert.Equal(WellKnownCommands.PlayerSettings, request.Command);
    }

    [CliTestFact]
    public void PlayerSettings_CreateSetRequest_SetsActionToSet()
    {
        var request = PlayerSettingsCommand.CreateSetRequest("companyName", "TestCo");
        Assert.Equal("set", request.Parameters!["action"]?.GetValue<string>());
    }

    [CliTestFact]
    public void PlayerSettings_CreateSetRequest_SetsKeyAndValue()
    {
        var request = PlayerSettingsCommand.CreateSetRequest("companyName", "MyStudio");
        Assert.Equal("companyName", request.Parameters!["key"]?.GetValue<string>());
        Assert.Equal("MyStudio", request.Parameters!["value"]?.GetValue<string>());
    }

    [CliTestFact]
    public void PlayerSettings_CreateSetRequest_EmptyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => PlayerSettingsCommand.CreateSetRequest("", "v"));
    }

    [CliTestFact]
    public void PlayerSettings_CreateSetRequest_NullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PlayerSettingsCommand.CreateSetRequest("k", null!));
    }

    // === AssetCommand ===

    [CliTestFact]
    public void Asset_CreateRefreshRequest_SetsCommandName()
    {
        var request = AssetCommand.CreateRefreshRequest();
        Assert.Equal(WellKnownCommands.AssetRefresh, request.Command);
    }

    [CliTestFact]
    public void Asset_CreateRefreshRequest_HasRequestId()
    {
        var request = AssetCommand.CreateRefreshRequest();
        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }

    [CliTestFact]
    public void Asset_CreateRefreshRequest_HasParameters()
    {
        var request = AssetCommand.CreateRefreshRequest();
        Assert.NotNull(request.Parameters);
    }
}
