using System.Text.Json;
using Unityctl.Shared;
using Unityctl.Shared.Commands;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Shared.Tests;

public class CommandSchemaTests
{
    [Fact]
    public void Schema_ContainsAllCatalogCommands()
    {
        var schema = new CommandSchema
        {
            Version = Constants.Version,
            Commands = CommandCatalog.All
        };

        Assert.Equal(CommandCatalog.All.Length, schema.Commands.Length);
    }

    [Fact]
    public void Schema_VersionMatchesConstants()
    {
        var schema = new CommandSchema
        {
            Version = Constants.Version,
            Commands = CommandCatalog.All
        };

        Assert.Equal(Constants.Version, schema.Version);
    }

    [Fact]
    public void Schema_SerializesWithSourceGenerator()
    {
        var schema = new CommandSchema
        {
            Version = Constants.Version,
            Commands = CommandCatalog.All
        };

        // Must not throw — uses Source Generator path
        var json = JsonSerializer.Serialize(schema, UnityctlJsonContext.Default.CommandSchema);

        Assert.NotEmpty(json);
        Assert.Contains("\"version\"", json);
        Assert.Contains("\"commands\"", json);
    }

    [Fact]
    public void Schema_RoundTrip_PreservesCommandCount()
    {
        var original = new CommandSchema
        {
            Version = Constants.Version,
            Commands = CommandCatalog.All
        };

        var json = JsonSerializer.Serialize(original, UnityctlJsonContext.Default.CommandSchema);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.CommandSchema);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Commands.Length, deserialized!.Commands.Length);
        Assert.Equal(original.Version, deserialized.Version);
    }

    [Fact]
    public void AllCommands_HaveNonEmptyDescriptions()
    {
        foreach (var cmd in CommandCatalog.All)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(cmd.Description),
                $"Command '{cmd.Name}' has empty description");
        }
    }

    [Fact]
    public void AllCommands_HaveNonEmptyCategories()
    {
        foreach (var cmd in CommandCatalog.All)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(cmd.Category),
                $"Command '{cmd.Name}' has empty category");
        }
    }

    [Fact]
    public void AllParameters_HaveTypes()
    {
        foreach (var cmd in CommandCatalog.All)
        {
            foreach (var param in cmd.Parameters)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(param.Type),
                    $"Parameter '{param.Name}' on command '{cmd.Name}' has empty type");
            }
        }
    }

    [Fact]
    public void AllParameters_HaveDescriptions()
    {
        foreach (var cmd in CommandCatalog.All)
        {
            foreach (var param in cmd.Parameters)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(param.Description),
                    $"Parameter '{param.Name}' on command '{cmd.Name}' has empty description");
            }
        }
    }

    [Fact]
    public void Schema_ContainsSchemaCommand()
    {
        Assert.Contains(CommandCatalog.All, c => c.Name == "schema");
    }

    [Fact]
    public void Schema_ContainsExecCommand()
    {
        Assert.Contains(CommandCatalog.All, c => c.Name == "exec");
    }

    [Fact]
    public void Schema_ContainsWorkflowCommand()
    {
        Assert.Contains(CommandCatalog.All, c => c.Name == "workflow");
    }
}
