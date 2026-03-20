#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class CinemachineSetPropertyHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.CinemachineSetProperty;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var vcamType = FindType("Unity.Cinemachine.CinemachineCamera")
                        ?? FindType("Cinemachine.CinemachineVirtualCamera");

            if (vcamType == null)
                return Fail(StatusCode.NotFound,
                    "Cinemachine not available. Install com.unity.cinemachine package.");

            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var propertyPath = request.GetParam("property", null);
            if (string.IsNullOrEmpty(propertyPath))
                return InvalidParameters("Parameter 'property' is required.");

            var valueStr = request.GetParam("value", null);
            if (valueStr == null)
                return InvalidParameters("Parameter 'value' is required.");

            var resolved = GlobalObjectIdResolver.Resolve(id);
            if (resolved == null)
                return Fail(StatusCode.NotFound, $"Object not found for id: {id}");

            Component vcam = null;
            if (vcamType.IsInstanceOfType(resolved))
                vcam = resolved as Component;
            else if (resolved is GameObject go)
                vcam = go.GetComponent(vcamType) as Component;

            if (vcam == null)
                return Fail(StatusCode.NotFound, $"No Cinemachine camera found on: {id}");

            var so = new SerializedObject(vcam);
            var prop = so.FindProperty(propertyPath);
            if (prop == null)
                return Fail(StatusCode.NotFound, $"Property '{propertyPath}' not found on {vcam.GetType().Name}.");

            Undo.RecordObject(vcam, $"unityctl: cinemachine-set-property: {propertyPath}");

            bool success;
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                    if (float.TryParse(valueStr, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var fval))
                    {
                        prop.floatValue = fval;
                        success = true;
                    }
                    else
                    {
                        return InvalidParameters($"Cannot parse '{valueStr}' as float.");
                    }
                    break;
                case SerializedPropertyType.Integer:
                    if (int.TryParse(valueStr, out var ival))
                    {
                        prop.intValue = ival;
                        success = true;
                    }
                    else
                    {
                        return InvalidParameters($"Cannot parse '{valueStr}' as int.");
                    }
                    break;
                case SerializedPropertyType.Boolean:
                    if (bool.TryParse(valueStr, out var bval))
                    {
                        prop.boolValue = bval;
                        success = true;
                    }
                    else
                    {
                        return InvalidParameters($"Cannot parse '{valueStr}' as bool.");
                    }
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = valueStr;
                    success = true;
                    break;
                case SerializedPropertyType.Enum:
                    if (int.TryParse(valueStr, out var enumIdx))
                    {
                        prop.enumValueIndex = enumIdx;
                        success = true;
                    }
                    else
                    {
                        var names = prop.enumDisplayNames;
                        var idx = Array.IndexOf(names, valueStr);
                        if (idx >= 0)
                        {
                            prop.enumValueIndex = idx;
                            success = true;
                        }
                        else
                        {
                            return InvalidParameters($"Enum value '{valueStr}' not found. Options: {string.Join(", ", names)}");
                        }
                    }
                    break;
                default:
                    return Fail(StatusCode.InvalidParameters,
                        $"Property type '{prop.propertyType}' is not supported for direct set.");
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(vcam);

            return Ok($"Set {propertyPath} = {valueStr} on '{vcam.gameObject.name}'", new JObject
            {
                ["globalObjectId"] = GlobalObjectIdResolver.GetId(vcam),
                ["gameObjectName"] = vcam.gameObject.name,
                ["property"] = propertyPath,
                ["value"] = valueStr
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
