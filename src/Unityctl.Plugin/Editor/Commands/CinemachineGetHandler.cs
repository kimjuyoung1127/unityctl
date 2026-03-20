#if UNITY_EDITOR
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class CinemachineGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.CinemachineGet;

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

            // Use SerializedObject to get all properties
            var so = new SerializedObject(vcam);
            var data = new JObject
            {
                ["globalObjectId"] = GlobalObjectIdResolver.GetId(vcam),
                ["gameObjectName"] = vcam.gameObject.name,
                ["type"] = vcam.GetType().Name,
                ["cinemachineVersion"] = vcam.GetType().Namespace == "Unity.Cinemachine" ? "3.x" : "2.x"
            };

            // Collect key properties via reflection
            var priority = GetPropertyValue(vcam, "Priority");
            if (priority != null) data["priority"] = JToken.FromObject(priority);

            // Lens properties — try property first, then field
            var lensPropInfo = vcam.GetType().GetProperty("m_Lens");
            var lensFieldInfo = vcam.GetType().GetField("m_Lens", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (lensPropInfo != null)
            {
                var lensVal = lensPropInfo.GetValue(vcam);
                if (lensVal != null)
                    data["lens"] = SerializeLens(lensVal);
            }
            else if (lensFieldInfo != null)
            {
                var lensVal = lensFieldInfo.GetValue(vcam);
                if (lensVal != null)
                    data["lens"] = SerializeLens(lensVal);
            }

            // Serialize via SerializedObject for complete view
            var properties = new JObject();
            var iter = so.GetIterator();
            if (iter.NextVisible(true))
            {
                int count = 0;
                do
                {
                    if (count++ > 50) break; // Safety limit
                    properties[iter.propertyPath] = iter.type switch
                    {
                        "float" => (JToken)iter.floatValue,
                        "int" => iter.intValue,
                        "bool" => iter.boolValue,
                        "string" => iter.stringValue,
                        "Enum" => iter.enumDisplayNames.Length > iter.enumValueIndex && iter.enumValueIndex >= 0
                            ? iter.enumDisplayNames[iter.enumValueIndex] : iter.enumValueIndex.ToString(),
                        _ => iter.type
                    };
                } while (iter.NextVisible(false));
            }

            data["serializedProperties"] = properties;
            return Ok($"Cinemachine camera details for '{vcam.gameObject.name}'", data);
        }

        private static JObject SerializeLens(object lens)
        {
            var result = new JObject();
            var fields = lens.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var val = field.GetValue(lens);
                if (val is float f) result[field.Name] = f;
                else if (val is int i) result[field.Name] = i;
                else if (val is bool b) result[field.Name] = b;
                else if (val != null) result[field.Name] = val.ToString();
            }
            return result;
        }

        private static object GetPropertyValue(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name);
            return prop?.GetValue(obj);
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
