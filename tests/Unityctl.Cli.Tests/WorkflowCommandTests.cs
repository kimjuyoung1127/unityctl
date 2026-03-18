using System.Text.Json;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Cli.Tests;

public class WorkflowCommandTests
{
    [CliTestFact]
    public void WorkflowDefinition_Deserialize_ValidJson()
    {
        const string json = """
        {
            "name": "build-and-test",
            "continueOnError": false,
            "steps": [
                { "command": "build", "project": "/MyProject", "parameters": { "target": "WebGL" } },
                { "command": "test", "project": "/MyProject" }
            ]
        }
        """;

        var workflow = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.WorkflowDefinition);

        Assert.NotNull(workflow);
        Assert.Equal("build-and-test", workflow!.Name);
        Assert.Equal(2, workflow.Steps.Length);
        Assert.False(workflow.ContinueOnError);
    }

    [CliTestFact]
    public void WorkflowStep_Deserialize_CommandAndProject()
    {
        const string json = """
        { "command": "ping", "project": "/path/to/project" }
        """;

        var step = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.WorkflowStep);

        Assert.NotNull(step);
        Assert.Equal("ping", step!.Command);
        Assert.Equal("/path/to/project", step.Project);
    }

    [CliTestFact]
    public void WorkflowDefinition_ContinueOnError_DefaultsFalse()
    {
        const string json = """{ "name": "test", "steps": [] }""";

        var workflow = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.WorkflowDefinition);

        Assert.NotNull(workflow);
        Assert.False(workflow!.ContinueOnError);
    }

    [CliTestFact]
    public void WorkflowDefinition_SerializeRoundTrip_PreservesData()
    {
        var original = new WorkflowDefinition
        {
            Name = "my-workflow",
            ContinueOnError = true,
            Steps =
            [
                new WorkflowStep { Command = "build", Project = "/proj", TimeoutSeconds = 60 }
            ]
        };

        var json = JsonSerializer.Serialize(original, UnityctlJsonContext.Default.WorkflowDefinition);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.WorkflowDefinition);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized!.Name);
        Assert.True(deserialized.ContinueOnError);
        Assert.Single(deserialized.Steps);
        Assert.Equal("build", deserialized.Steps[0].Command);
        Assert.Equal(60, deserialized.Steps[0].TimeoutSeconds);
    }

    [CliTestFact]
    public void WorkflowStep_TimeoutSeconds_Null_WhenOmitted()
    {
        const string json = """{ "command": "check", "project": "/proj" }""";

        var step = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.WorkflowStep);

        Assert.NotNull(step);
        Assert.Null(step!.TimeoutSeconds);
    }
}
