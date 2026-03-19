using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptGetErrorsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptGetErrors;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var filterPath = request.GetParam("path", null);

            var result = ScriptCompilationCollector.GetLatestResult();
            if (result == null)
            {
                return Ok("No compilation data available yet. Wait for the Editor to finish compiling, then run 'script-validate --wait' once if needed.", new JObject
                {
                    ["errors"] = new JArray(),
                    ["warnings"] = new JArray(),
                    ["errorCount"] = 0,
                    ["warningCount"] = 0,
                    ["isStale"] = true,
                    ["state"] = "no-compilation-data",
                    ["recommendedAction"] = "Wait for Unity to report Ready, then run 'unityctl script validate --project <path> --wait' if compile diagnostics are still missing."
                });
            }

            // Apply path filter if specified
            if (!string.IsNullOrEmpty(filterPath))
            {
                var filteredErrors = FilterByPath(result["errors"] as JArray, filterPath);
                var filteredWarnings = FilterByPath(result["warnings"] as JArray, filterPath);

                return Ok($"Found {filteredErrors.Count} error(s), {filteredWarnings.Count} warning(s) for {filterPath}", new JObject
                {
                    ["errors"] = filteredErrors,
                    ["warnings"] = filteredWarnings,
                    ["errorCount"] = filteredErrors.Count,
                    ["warningCount"] = filteredWarnings.Count,
                    ["compiledAt"] = result["compiledAt"],
                    ["filter"] = filterPath,
                    ["isStale"] = result["isStale"]
                });
            }

            var errorCount = result["errorCount"]?.Value<int>() ?? 0;
            var warningCount = result["warningCount"]?.Value<int>() ?? 0;

            return Ok($"Found {errorCount} error(s), {warningCount} warning(s)", result);
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static JArray FilterByPath(JArray items, string path)
        {
            var filtered = new JArray();
            if (items == null) return filtered;

            foreach (var item in items)
            {
                var file = item["file"]?.Value<string>();
                if (file != null && file.IndexOf(path, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    filtered.Add(item.DeepClone());
            }
            return filtered;
        }
#endif
    }
}
