using Unityctl.Shared;
using Xunit;

namespace Unityctl.Core.Tests;

public class PipeNameTests
{
    [Fact]
    public void GetPipeName_StartsWithPrefix()
    {
        var name = Constants.GetPipeName("/some/project");
        Assert.StartsWith("unityctl_", name);
    }

    [Fact]
    public void GetPipeName_DeterministicForSamePath()
    {
        var name1 = Constants.GetPipeName("/some/project");
        var name2 = Constants.GetPipeName("/some/project");
        Assert.Equal(name1, name2);
    }

    [Fact]
    public void GetPipeName_DifferentForDifferentPaths()
    {
        var name1 = Constants.GetPipeName("/project/a");
        var name2 = Constants.GetPipeName("/project/b");
        Assert.NotEqual(name1, name2);
    }

    [Fact]
    public void NormalizeProjectPath_TrimsTrailingSlashes()
    {
        var withSlash = Constants.NormalizeProjectPath("/some/project/");
        var withMultiple = Constants.NormalizeProjectPath("/some/project///");
        Assert.False(withSlash.EndsWith("/"));
        Assert.False(withMultiple.EndsWith("/"));
        Assert.Equal(withSlash, withMultiple);
    }

    [Fact]
    public void NormalizeProjectPath_UnifiesSlashes()
    {
        var normalized = Constants.NormalizeProjectPath("/some/project");
        Assert.DoesNotContain("\\", normalized);
    }

    [Fact]
    public void GetPipeName_HasCorrectLength()
    {
        var name = Constants.GetPipeName("/some/project");
        // "unityctl_" (9 chars) + 16 hex chars = 25
        Assert.Equal(25, name.Length);
    }
}
