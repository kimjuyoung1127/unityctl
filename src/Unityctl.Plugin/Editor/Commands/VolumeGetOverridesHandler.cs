#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class VolumeGetOverridesHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.VolumeGetOverrides;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var volumeType = FindType("UnityEngine.Rendering.Volume");
            if (volumeType == null)
                return Fail(StatusCode.NotFound,
                    "Volume API not available. Install com.unity.render-pipelines.universal or .high-definition");

            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var componentName = request.GetParam("component", null);
            if (string.IsNullOrEmpty(componentName))
                return InvalidParameters("Parameter 'component' is required.");

            var resolved = GlobalObjectIdResolver.Resolve(id);
            if (resolved == null)
                return Fail(StatusCode.NotFound, $"Object not found for id: {id}");

            Component volume = null;
            if (volumeType.IsInstanceOfType(resolved))
                volume = resolved as Component;
            else if (resolved is GameObject go)
                volume = go.GetComponent(volumeType) as Component;

            if (volume == null)
                return Fail(StatusCode.NotFound, $"No Volume component found on: {id}");

            var profileProp = volume.GetType().GetProperty("profile");
            var profile = profileProp?.GetValue(volume);
            if (profile == null)
                return Fail(StatusCode.NotFound, "Volume has no profile assigned.");

            var componentsProp = profile.GetType().GetProperty("components");
            var components = componentsProp?.GetValue(profile) as IList;
            if (components == null)
                return Fail(StatusCode.NotFound, "Volume profile has no components.");

            object targetVc = null;
            foreach (var vc in components)
            {
                if (vc.GetType().Name.Equals(componentName, StringComparison.OrdinalIgnoreCase) ||
                    (vc.GetType().FullName != null && vc.GetType().FullName.Equals(componentName, StringComparison.OrdinalIgnoreCase)))
                {
                    targetVc = vc;
                    break;
                }
            }

            if (targetVc == null)
                return Fail(StatusCode.NotFound, $"VolumeComponent '{componentName}' not found in profile.");

            var parameters = new JArray();
            var fields = targetVc.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            var volumeParamType = FindType("UnityEngine.Rendering.VolumeParameter");

            foreach (var field in fields)
            {
                if (volumeParamType == null || !volumeParamType.IsAssignableFrom(field.FieldType))
                    continue;

                var paramObj = field.GetValue(targetVc);
                if (paramObj == null) continue;

                var valueProp = paramObj.GetType().GetProperty("value");
                var overrideStateProp = paramObj.GetType().GetProperty("overrideState");

                var value = valueProp?.GetValue(paramObj);
                var overrideState = overrideStateProp != null && (bool)overrideStateProp.GetValue(paramObj);

                parameters.Add(new JObject
                {
                    ["name"] = field.Name,
                    ["type"] = valueProp?.PropertyType.Name ?? "unknown",
                    ["value"] = value?.ToString() ?? "null",
                    ["overrideState"] = overrideState
                });
            }

            return Ok($"{componentName} has {parameters.Count} parameter(s)", new JObject
            {
                ["globalObjectId"] = GlobalObjectIdResolver.GetId(volume),
                ["component"] = targetVc.GetType().Name,
                ["fullType"] = targetVc.GetType().FullName,
                ["parameters"] = parameters
            });
        }

        private static Type FindType(string typeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }
    }
}
#endif
