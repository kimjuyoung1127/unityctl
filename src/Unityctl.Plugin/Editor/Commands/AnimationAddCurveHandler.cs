#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AnimationAddCurveHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AnimationAddCurve;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var bindingJson = request.GetParam("binding", null);
            if (string.IsNullOrEmpty(bindingJson))
                return InvalidParameters("Parameter 'binding' is required.");

            var keysJson = request.GetParam("keys", null);
            if (string.IsNullOrEmpty(keysJson))
                return InvalidParameters("Parameter 'keys' is required.");

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
                return Fail(StatusCode.NotFound, $"AnimationClip not found at: {path}");

            JObject bindingObj;
            try
            {
                bindingObj = JObject.Parse(bindingJson);
            }
            catch
            {
                return InvalidParameters("Parameter 'binding' must be valid JSON.");
            }

            var bindingPath = bindingObj.Value<string>("path") ?? string.Empty;
            var bindingTypeName = bindingObj.Value<string>("type");
            var propertyName = bindingObj.Value<string>("propertyName");

            if (string.IsNullOrEmpty(bindingTypeName))
                return InvalidParameters("binding.type is required.");
            if (string.IsNullOrEmpty(propertyName))
                return InvalidParameters("binding.propertyName is required.");

            var bindingType = ResolveType(bindingTypeName);
            if (bindingType == null)
                return Fail(StatusCode.NotFound, $"Type not found: {bindingTypeName}");

            JArray keysArr;
            try
            {
                keysArr = JArray.Parse(keysJson);
            }
            catch
            {
                return InvalidParameters("Parameter 'keys' must be a valid JSON array.");
            }

            if (keysArr.Count == 0)
                return InvalidParameters("Parameter 'keys' must have at least one keyframe.");

            var keyframes = new Keyframe[keysArr.Count];
            for (int i = 0; i < keysArr.Count; i++)
            {
                var kf = keysArr[i] as JObject;
                if (kf == null)
                    return InvalidParameters($"keys[{i}] must be an object with 'time' and 'value'.");

                keyframes[i] = new Keyframe(
                    kf.Value<float>("time"),
                    kf.Value<float>("value"));
            }

            var binding = EditorCurveBinding.FloatCurve(bindingPath, bindingType, propertyName);
            var curve = new AnimationCurve(keyframes);

            Undo.RecordObject(clip, "Add Animation Curve");
            AnimationUtility.SetEditorCurve(clip, binding, curve);
            EditorUtility.SetDirty(clip);

            return Ok($"Added curve '{propertyName}' to '{path}' with {keyframes.Length} keyframe(s)", new JObject
            {
                ["path"] = path,
                ["binding"] = new JObject
                {
                    ["path"] = bindingPath,
                    ["type"] = bindingTypeName,
                    ["propertyName"] = propertyName
                },
                ["keyframeCount"] = keyframes.Length
            });
        }

        private static System.Type ResolveType(string typeName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }
    }
}
#endif
