using Unityctl.Cli.Commands;
using Unityctl.Shared;
using Unityctl.Shared.Commands;
using Xunit;

namespace Unityctl.Cli.Tests;

public class SchemaCommandTests
{
    [CliTestFact]
    public void BuildSchema_ReturnsNonEmpty()
    {
        var schema = SchemaCommand.BuildSchema();

        Assert.NotNull(schema);
        Assert.NotEmpty(schema.Commands);
    }

    [CliTestFact]
    public void BuildSchema_VersionMatchesConstants()
    {
        var schema = SchemaCommand.BuildSchema();

        Assert.Equal(Constants.Version, schema.Version);
    }

    [CliTestFact]
    public void BuildSchema_ContainsAllCatalogCommands()
    {
        var schema = SchemaCommand.BuildSchema();

        Assert.Equal(CommandCatalog.All.Length, schema.Commands.Length);
    }

    [CliTestFact]
    public void BuildSchema_IncludesBuild()
    {
        var schema = SchemaCommand.BuildSchema();

        Assert.Contains(schema.Commands, c => c.Name == "build");
    }

    [CliTestFact]
    public void BuildSchema_IncludesSchema()
    {
        var schema = SchemaCommand.BuildSchema();

        Assert.Contains(schema.Commands, c => c.Name == "schema");
    }

    [CliTestFact]
    public void BuildSchema_IncludesExec()
    {
        var schema = SchemaCommand.BuildSchema();

        Assert.Contains(schema.Commands, c => c.Name == "exec");
    }

    [CliTestFact]
    public void BuildSchema_IncludesWorkflow()
    {
        var schema = SchemaCommand.BuildSchema();

        Assert.Contains(schema.Commands, c => c.Name == "workflow");
    }
}
