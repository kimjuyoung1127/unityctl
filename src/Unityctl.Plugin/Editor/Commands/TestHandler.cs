using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class TestHandler : IUnityctlCommand
    {
        public string CommandName => "test";

        public CommandResponse Execute(CommandRequest request)
        {
#if UNITY_EDITOR
            try
            {
                var mode = request.GetParam("mode", "edit");
                var filter = request.GetParam("filter", null);

                UnityEngine.Debug.Log($"[unityctl] Running tests: mode={mode}, filter={filter ?? "(all)"}");

                var testMode = mode.ToLowerInvariant() switch
                {
                    "edit" or "editmode" => UnityEngine.TestTools.TestPlatform.EditMode,
                    "play" or "playmode" => UnityEngine.TestTools.TestPlatform.PlayMode,
                    _ => UnityEngine.TestTools.TestPlatform.EditMode
                };

                var api = UnityEditor.TestTools.TestRunner.Api.ScriptableObject
                    .CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();

                var executionSettings = new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings
                {
                    filters = new[]
                    {
                        new UnityEditor.TestTools.TestRunner.Api.Filter
                        {
                            testMode = testMode,
                            testNames = string.IsNullOrEmpty(filter) ? null : new[] { filter }
                        }
                    }
                };

                var resultCollector = new TestResultCollector();
                api.RegisterCallbacks(resultCollector);
                api.Execute(executionSettings);

                var data = new JObject
                {
                    ["mode"] = mode,
                    ["filter"] = filter ?? "(all)"
                };
                return CommandResponse.Ok($"Tests started (mode={mode})", data);
            }
            catch (Exception e)
            {
                return CommandResponse.Fail(StatusCode.TestFailed,
                    $"Test execution failed: {e.Message}",
                    new List<string> { e.StackTrace });
            }
#else
            return CommandResponse.Fail(StatusCode.UnknownError, "Not running in Unity Editor");
#endif
        }
    }

#if UNITY_EDITOR
    internal class TestResultCollector : UnityEditor.TestTools.TestRunner.Api.ICallbacks
    {
        public int Passed { get; private set; }
        public int Failed { get; private set; }
        public int Skipped { get; private set; }
        public List<string> Failures { get; } = new();

        public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor testsToRun) { }

        public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
        {
            UnityEngine.Debug.Log($"[unityctl] Tests finished: Passed={Passed}, Failed={Failed}, Skipped={Skipped}");
        }

        public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor test) { }

        public void TestFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
        {
            switch (result.TestStatus)
            {
                case UnityEditor.TestTools.TestRunner.Api.TestStatus.Passed:
                    Passed++;
                    break;
                case UnityEditor.TestTools.TestRunner.Api.TestStatus.Failed:
                    Failed++;
                    Failures.Add($"{result.Test.FullName}: {result.Message}");
                    break;
                case UnityEditor.TestTools.TestRunner.Api.TestStatus.Skipped:
                    Skipped++;
                    break;
            }
        }
    }
#endif
}
