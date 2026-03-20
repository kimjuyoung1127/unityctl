#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ModelGetImportSettingsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ModelGetImportSettings;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                return Fail(StatusCode.NotFound, $"ModelImporter not found at: {path}");

            var data = new JObject
            {
                ["path"] = path,
                ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["globalScale"] = importer.globalScale,
                ["useFileScale"] = importer.useFileScale,
                ["meshCompression"] = importer.meshCompression.ToString(),
                ["isReadable"] = importer.isReadable,
                ["importAnimation"] = importer.importAnimation,
                ["importBlendShapes"] = importer.importBlendShapes,
                ["importNormals"] = importer.importNormals.ToString(),
                ["importTangents"] = importer.importTangents.ToString(),
                ["materialImportMode"] = importer.materialImportMode.ToString(),
                ["animationType"] = importer.animationType.ToString(),
                ["importConstraints"] = importer.importConstraints,
                ["importLights"] = importer.importLights,
                ["importCameras"] = importer.importCameras,
                ["importVisibility"] = importer.importVisibility
            };

            return Ok($"Model import settings for '{path}'", data);
        }
    }
}
#endif
