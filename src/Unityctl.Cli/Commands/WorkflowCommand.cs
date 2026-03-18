using System.Text.Json;
using Unityctl.Cli.Execution;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Cli.Commands;

/// <summary>
/// Executes a sequential workflow of unityctl commands from a JSON definition file.
/// Supports continueOnError for fault-tolerant batch execution.
/// </summary>
public static class WorkflowCommand
{
    public static void Run(string file, string? project = null, bool json = false)
    {
        var exitCode = RunAsync(file, project, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> RunAsync(string file, string? project, bool json)
    {
        WorkflowDefinition? workflow;
        try
        {
            var content = await File.ReadAllTextAsync(file);
            workflow = JsonSerializer.Deserialize(content, UnityctlJsonContext.Default.WorkflowDefinition);
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine($"Error: Workflow file not found: {file}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading workflow file: {ex.Message}");
            return 1;
        }

        if (workflow == null || workflow.Steps.Length == 0)
        {
            Console.Error.WriteLine("Error: Workflow file is empty or invalid.");
            return 1;
        }

        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        var results = new List<CommandResponse>();
        var anyFailed = false;

        foreach (var step in workflow.Steps)
        {
            var stepProject = step.Project ?? project;
            if (string.IsNullOrWhiteSpace(stepProject))
            {
                Console.Error.WriteLine(
                    $"Error: Step '{step.Command}' has no project path. " +
                    "Provide --project or set 'project' in the step definition.");
                if (!workflow.ContinueOnError) return 1;
                anyFailed = true;
                continue;
            }

            var request = new CommandRequest
            {
                Command = step.Command,
                Parameters = step.Parameters
            };

            CancellationToken ct = default;
            if (step.TimeoutSeconds.HasValue)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(step.TimeoutSeconds.Value));
                ct = cts.Token;
            }

            CommandResponse response;
            try
            {
                response = await executor.ExecuteAsync(stepProject, request, ct: ct);
            }
            catch (OperationCanceledException)
            {
                response = CommandResponse.Fail(
                    StatusCode.UnknownError,
                    $"Step '{step.Command}' timed out after {step.TimeoutSeconds}s.");
            }

            results.Add(response);

            if (!json)
                CommandRunner.PrintResponse(response, json: false);

            if (!response.Success)
            {
                anyFailed = true;
                if (!workflow.ContinueOnError)
                    break;
            }
        }

        if (json)
        {
            Console.WriteLine("[");
            for (var i = 0; i < results.Count; i++)
            {
                var suffix = i < results.Count - 1 ? "," : string.Empty;
                Console.WriteLine(
                    JsonSerializer.Serialize(results[i], UnityctlJsonContext.Default.CommandResponse) + suffix);
            }
            Console.WriteLine("]");
        }

        return anyFailed ? 1 : 0;
    }
}
