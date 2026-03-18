using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PlayerSettingsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PlayerSettings;

        private static readonly HashSet<string> AllowedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "companyName",
            "productName",
            "bundleVersion",
            "defaultScreenWidth",
            "defaultScreenHeight",
            "runInBackground",
            "colorSpace",
            "scriptingBackend",
            "apiCompatibilityLevel",
            "applicationIdentifier"
        };

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var action = request.GetParam("action", "get").ToLowerInvariant();
            var key = request.GetParam("key", null);

            if (string.IsNullOrEmpty(key))
                return InvalidParameters("Parameter 'key' is required.");

            if (!AllowedKeys.Contains(key))
                return InvalidParameters(
                    $"Unknown key: '{key}'. Allowed keys: {string.Join(", ", AllowedKeys)}");

            switch (action)
            {
                case "get":
                    return GetProperty(key);
                case "set":
                    var value = request.GetParam("value", null);
                    if (value == null)
                        return InvalidParameters("Parameter 'value' is required for set action.");
                    return SetProperty(key, value);
                default:
                    return InvalidParameters(
                        $"Unknown action: '{action}'. Valid actions: get, set");
            }
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private CommandResponse GetProperty(string key)
        {
            var value = ReadProperty(key);
            if (value == null)
                return Fail(StatusCode.UnknownError, $"Failed to read property: {key}");

            return Ok($"{key} = {value}", new JObject
            {
                ["key"] = key,
                ["value"] = value
            });
        }

        private CommandResponse SetProperty(string key, string value)
        {
            var so = GetPlayerSettingsSerializedObject();
            if (so == null)
                return Fail(StatusCode.UnknownError, "Failed to access PlayerSettings backing object.");

            // Record undo before modification
            UnityEditor.Undo.RecordObject(so.targetObject, $"unityctl: player-settings set {key}");

            var prop = so.FindProperty(MapKeyToSerializedProperty(key));
            if (prop == null)
            {
                // Fallback to direct API for properties not in SerializedObject
                return SetPropertyDirect(key, value);
            }

            if (!SetSerializedPropertyValue(prop, value))
                return InvalidParameters($"Failed to set value '{value}' for property '{key}'.");

            so.ApplyModifiedProperties();

            var readBack = ReadProperty(key);
            return Ok($"{key} set to {readBack}", new JObject
            {
                ["key"] = key,
                ["value"] = readBack,
                ["undoGroupName"] = $"unityctl: player-settings set {key}"
            });
        }

        private static string ReadProperty(string key)
        {
            switch (key.ToLowerInvariant())
            {
                case "companyname": return UnityEditor.PlayerSettings.companyName;
                case "productname": return UnityEditor.PlayerSettings.productName;
                case "bundleversion": return UnityEditor.PlayerSettings.bundleVersion;
                case "defaultscreenwidth": return UnityEditor.PlayerSettings.defaultScreenWidth.ToString();
                case "defaultscreenheight": return UnityEditor.PlayerSettings.defaultScreenHeight.ToString();
                case "runinbackground": return UnityEditor.PlayerSettings.runInBackground.ToString();
                case "colorspace": return UnityEditor.PlayerSettings.colorSpace.ToString();
                case "scriptingbackend":
                    var group = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
                    return UnityEditor.PlayerSettings.GetScriptingBackend(group).ToString();
                case "apicompatibilitylevel":
                    var g2 = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
                    return UnityEditor.PlayerSettings.GetApiCompatibilityLevel(g2).ToString();
                case "applicationidentifier":
                    return UnityEditor.PlayerSettings.applicationIdentifier;
                default: return null;
            }
        }

        private static CommandResponse SetPropertyDirect(string key, string value)
        {
            try
            {
                switch (key.ToLowerInvariant())
                {
                    case "companyname":
                        UnityEditor.PlayerSettings.companyName = value;
                        break;
                    case "productname":
                        UnityEditor.PlayerSettings.productName = value;
                        break;
                    case "bundleversion":
                        UnityEditor.PlayerSettings.bundleVersion = value;
                        break;
                    case "defaultscreenwidth":
                        UnityEditor.PlayerSettings.defaultScreenWidth = int.Parse(value);
                        break;
                    case "defaultscreenheight":
                        UnityEditor.PlayerSettings.defaultScreenHeight = int.Parse(value);
                        break;
                    case "runinbackground":
                        UnityEditor.PlayerSettings.runInBackground = bool.Parse(value);
                        break;
                    case "colorspace":
                        UnityEditor.PlayerSettings.colorSpace = (UnityEngine.ColorSpace)Enum.Parse(
                            typeof(UnityEngine.ColorSpace), value, true);
                        break;
                    case "applicationidentifier":
                        UnityEditor.PlayerSettings.applicationIdentifier = value;
                        break;
                    default:
                        return CommandResponse.Fail(StatusCode.InvalidParameters,
                            $"Direct set not supported for: {key}");
                }

                var readBack = ReadProperty(key);
                return CommandResponse.Ok($"{key} set to {readBack}", new JObject
                {
                    ["key"] = key,
                    ["value"] = readBack,
                    ["undoGroupName"] = $"unityctl: player-settings set {key}"
                });
            }
            catch (Exception e)
            {
                return CommandResponse.Fail(StatusCode.InvalidParameters,
                    $"Failed to set '{key}': {e.Message}");
            }
        }

        private static UnityEditor.SerializedObject GetPlayerSettingsSerializedObject()
        {
            try
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                    "ProjectSettings/ProjectSettings.asset");
                if (assets == null || assets.Length == 0) return null;

                foreach (var asset in assets)
                {
                    if (asset is UnityEditor.PlayerSettings)
                        return new UnityEditor.SerializedObject(asset);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string MapKeyToSerializedProperty(string key)
        {
            // SerializedObject property paths for PlayerSettings
            switch (key.ToLowerInvariant())
            {
                case "companyname": return "companyName";
                case "productname": return "productName";
                case "bundleversion": return "bundleVersion";
                case "defaultscreenwidth": return "defaultScreenWidth";
                case "defaultscreenheight": return "defaultScreenHeight";
                case "runinbackground": return "runInBackground";
                default: return key;
            }
        }

        private static bool SetSerializedPropertyValue(UnityEditor.SerializedProperty prop, string value)
        {
            try
            {
                switch (prop.propertyType)
                {
                    case UnityEditor.SerializedPropertyType.String:
                        prop.stringValue = value;
                        return true;
                    case UnityEditor.SerializedPropertyType.Integer:
                        prop.intValue = int.Parse(value);
                        return true;
                    case UnityEditor.SerializedPropertyType.Boolean:
                        prop.boolValue = bool.Parse(value);
                        return true;
                    case UnityEditor.SerializedPropertyType.Float:
                        prop.floatValue = float.Parse(value);
                        return true;
                    case UnityEditor.SerializedPropertyType.Enum:
                        prop.enumValueIndex = int.Parse(value);
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }
#endif
    }
}
