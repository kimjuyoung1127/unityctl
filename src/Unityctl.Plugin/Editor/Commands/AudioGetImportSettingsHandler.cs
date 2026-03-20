#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AudioGetImportSettingsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AudioGetImportSettings;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
                return Fail(StatusCode.NotFound, $"AudioImporter not found at: {path}");

            var defaultSettings = importer.defaultSampleSettings;

            var data = new JObject
            {
                ["path"] = path,
                ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["forceToMono"] = importer.forceToMono,
                ["loadInBackground"] = importer.loadInBackground,
                ["ambisonic"] = importer.ambisonic,
                ["preloadAudioData"] = defaultSettings.preloadAudioData,
                ["defaultSampleSettings"] = new JObject
                {
                    ["loadType"] = defaultSettings.loadType.ToString(),
                    ["compressionFormat"] = defaultSettings.compressionFormat.ToString(),
                    ["quality"] = defaultSettings.quality,
                    ["sampleRateSetting"] = defaultSettings.sampleRateSetting.ToString()
                }
            };

            return Ok($"Audio import settings for '{path}'", data);
        }
    }
}
#endif
