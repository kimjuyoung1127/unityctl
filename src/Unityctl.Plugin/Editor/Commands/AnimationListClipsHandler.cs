#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AnimationListClipsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AnimationListClips;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var folder = request.GetParam("folder", null);
            var filter = request.GetParam("filter", null);
            var limit = request.GetParam<int>("limit");

            var searchFilter = "t:AnimationClip";
            if (!string.IsNullOrEmpty(filter))
                searchFilter += " " + filter;

            string[] guids;
            if (string.IsNullOrWhiteSpace(folder))
                guids = AssetDatabase.FindAssets(searchFilter);
            else
                guids = AssetDatabase.FindAssets(searchFilter, new[] { folder });

            var results = new JArray();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(path);
                if (clip == null) continue;

                results.Add(new JObject
                {
                    ["guid"] = guid,
                    ["path"] = path,
                    ["name"] = clip.name,
                    ["length"] = clip.length,
                    ["frameRate"] = clip.frameRate,
                    ["isLooping"] = clip.isLooping
                });

                if (limit > 0 && results.Count >= limit)
                    break;
            }

            return Ok($"Found {results.Count} AnimationClip(s)", new JObject
            {
                ["results"] = results
            });
        }
    }
}
#endif
