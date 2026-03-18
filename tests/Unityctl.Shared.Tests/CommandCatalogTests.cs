using Unityctl.Shared.Commands;
using Xunit;

namespace Unityctl.Shared.Tests;

public class CommandCatalogTests
{
    [Fact]
    public void All_HasStableCommandNames()
    {
        var names = CommandCatalog.All.Select(command => command.Name).ToArray();

        Assert.Equal(
            ["init", "editor list", "ping", "status", "build", "test", "check", "tools", "log",
             "session list", "session stop", "session clean", "watch",
             "scene snapshot", "scene diff"],
            names);
    }

    [Fact]
    public void All_HasNoDuplicateNames()
    {
        var names = CommandCatalog.All.Select(command => command.Name).ToArray();

        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void RequiredParameters_AreMarkedCorrectly()
    {
        var init = CommandCatalog.All.Single(command => command.Name == "init");
        var status = CommandCatalog.All.Single(command => command.Name == "status");

        Assert.Contains(init.Parameters, parameter => parameter.Name == "project" && parameter.Required);
        Assert.Contains(status.Parameters, parameter => parameter.Name == "project" && parameter.Required);
        Assert.DoesNotContain(status.Parameters, parameter => parameter.Name == "json" && parameter.Required);
    }

    [Fact]
    public void Build_HasDryRunParameter_AsOptional()
    {
        var build = CommandCatalog.All.Single(command => command.Name == "build");

        Assert.Contains(build.Parameters, p => p.Name == "dryRun");
        Assert.DoesNotContain(build.Parameters, p => p.Name == "dryRun" && p.Required);
    }

    [Fact]
    public void SceneSnapshot_HasProjectParameter_AsRequired()
    {
        var sceneSnapshot = CommandCatalog.All.Single(command => command.Name == "scene snapshot");

        Assert.Contains(sceneSnapshot.Parameters, p => p.Name == "project" && p.Required);
        Assert.Contains(sceneSnapshot.Parameters, p => p.Name == "scenePath" && !p.Required);
    }

    [Fact]
    public void SceneDiff_HasEpsilonParameter_AsOptional()
    {
        var sceneDiff = CommandCatalog.All.Single(command => command.Name == "scene diff");

        Assert.Contains(sceneDiff.Parameters, p => p.Name == "epsilon");
        Assert.DoesNotContain(sceneDiff.Parameters, p => p.Name == "epsilon" && p.Required);
    }

    [Fact]
    public void SceneDiff_HasLiveParameter_AsOptional()
    {
        var sceneDiff = CommandCatalog.All.Single(command => command.Name == "scene diff");

        Assert.Contains(sceneDiff.Parameters, p => p.Name == "live");
        Assert.DoesNotContain(sceneDiff.Parameters, p => p.Name == "live" && p.Required);
    }
}
