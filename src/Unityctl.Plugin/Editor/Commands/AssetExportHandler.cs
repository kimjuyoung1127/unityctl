#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetExportHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetExport;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var pathsCsv = request.GetParam("paths", null);
            if (string.IsNullOrEmpty(pathsCsv))
                return InvalidParameters("Parameter 'paths' is required.");

            var output = request.GetParam("output", null);
            if (string.IsNullOrEmpty(output))
                return InvalidParameters("Parameter 'output' is required.");

            var includeDependencies = request.GetParam("includeDependencies", true);

            var paths = pathsCsv.Split(',');
            for (int i = 0; i < paths.Length; i++)
                paths[i] = paths[i].Trim();

            // Validate that at least one asset exists
            foreach (var path in paths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                    return Fail(StatusCode.NotFound, $"Asset not found at: {path}");
            }

            var outputDir = System.IO.Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(outputDir) && !System.IO.Directory.Exists(outputDir))
                return Fail(StatusCode.InvalidParameters, $"Output directory does not exist: {outputDir}");

            var options = includeDependencies
                ? ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse
                : ExportPackageOptions.Recurse;

            AssetDatabase.ExportPackage(paths, output, options);

            return Ok($"Exported {paths.Length} asset(s) to '{output}'", new JObject
            {
                ["output"] = output,
                ["paths"] = new JArray(paths),
                ["includeDependencies"] = includeDependencies
            });
        }
    }
}
#endif
