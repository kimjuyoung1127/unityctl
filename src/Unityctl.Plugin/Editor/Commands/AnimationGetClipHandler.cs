#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AnimationGetClipHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AnimationGetClip;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
                return Fail(StatusCode.NotFound, $"AnimationClip not found at: {path}");

            var bindings = AnimationUtility.GetCurveBindings(clip);
            var curves = new JArray();
            foreach (var binding in bindings)
            {
                curves.Add(new JObject
                {
                    ["path"] = binding.path,
                    ["type"] = binding.type.FullName ?? binding.type.Name,
                    ["propertyName"] = binding.propertyName
                });
            }

            var events = AnimationUtility.GetAnimationEvents(clip);
            var eventsArr = new JArray();
            foreach (var evt in events)
            {
                eventsArr.Add(new JObject
                {
                    ["time"] = evt.time,
                    ["functionName"] = evt.functionName,
                    ["intParameter"] = evt.intParameter,
                    ["floatParameter"] = evt.floatParameter,
                    ["stringParameter"] = evt.stringParameter ?? string.Empty
                });
            }

            var data = new JObject
            {
                ["path"] = path,
                ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["name"] = clip.name,
                ["length"] = clip.length,
                ["frameRate"] = clip.frameRate,
                ["isLooping"] = clip.isLooping,
                ["wrapMode"] = clip.wrapMode.ToString(),
                ["curveCount"] = bindings.Length,
                ["curves"] = curves,
                ["eventCount"] = events.Length,
                ["events"] = eventsArr
            };

            return Ok($"AnimationClip details for '{path}'", data);
        }
    }
}
#endif
