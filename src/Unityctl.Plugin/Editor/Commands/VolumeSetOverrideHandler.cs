#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class VolumeSetOverrideHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.VolumeSetOverride;

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

            var propertyName = request.GetParam("property", null);
            if (string.IsNullOrEmpty(propertyName))
                return InvalidParameters("Parameter 'property' is required.");

            var valueStr = request.GetParam("value", null);
            if (valueStr == null)
                return InvalidParameters("Parameter 'value' is required.");

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

            var paramField = targetVc.GetType().GetField(propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (paramField == null)
                return Fail(StatusCode.NotFound, $"Parameter '{propertyName}' not found on {componentName}.");

            var paramObj = paramField.GetValue(targetVc);
            if (paramObj == null)
                return Fail(StatusCode.NotFound, $"Parameter '{propertyName}' is null on {componentName}.");

            // VolumeParameter has 'value' property and 'overrideState'
            var valueProp = paramObj.GetType().GetProperty("value");
            var overrideStateProp = paramObj.GetType().GetProperty("overrideState");

            if (valueProp == null)
                return Fail(StatusCode.NotFound, $"Parameter '{propertyName}' does not have a value property.");

            Undo.RecordObject(volume, $"unityctl: volume-set-override: {propertyName}");

            // Set override state to true
            if (overrideStateProp != null && overrideStateProp.CanWrite)
                overrideStateProp.SetValue(paramObj, true);

            // Try to set value based on type
            var valueType = valueProp.PropertyType;
            object parsedValue;
            try
            {
                parsedValue = ConvertValue(valueStr, valueType);
            }
            catch (Exception ex)
            {
                return InvalidParameters($"Cannot convert '{valueStr}' to {valueType.Name}: {ex.Message}");
            }

            valueProp.SetValue(paramObj, parsedValue);
            EditorUtility.SetDirty(volume);

            return Ok($"Set {componentName}.{propertyName} = {valueStr}", new JObject
            {
                ["globalObjectId"] = GlobalObjectIdResolver.GetId(volume),
                ["component"] = componentName,
                ["property"] = propertyName,
                ["value"] = valueStr,
                ["overrideEnabled"] = true
            });
        }

        private static object ConvertValue(string str, Type targetType)
        {
            if (targetType == typeof(float)) return float.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(int)) return int.Parse(str);
            if (targetType == typeof(bool)) return bool.Parse(str);
            if (targetType == typeof(string)) return str;
            if (targetType.IsEnum) return Enum.Parse(targetType, str, true);
            return Convert.ChangeType(str, targetType, System.Globalization.CultureInfo.InvariantCulture);
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
