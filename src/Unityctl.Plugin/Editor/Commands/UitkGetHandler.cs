#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UitkGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UitkGet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var uiDocType = FindType("UnityEngine.UIElements.UIDocument");
            if (uiDocType == null)
                return Fail(StatusCode.NotFound, "UI Toolkit (UIDocument) not available in this Unity version.");

            var name = request.GetParam("name", null);
            if (string.IsNullOrEmpty(name))
                return InvalidParameters("Parameter 'name' is required.");

            var docs = UnityEngine.Object.FindObjectsByType(uiDocType,
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var doc in docs)
            {
                var rootProp = doc.GetType().GetProperty("rootVisualElement");
                var root = rootProp?.GetValue(doc);
                if (root == null) continue;

                // Use Q method to find by name
                var qMethod = root.GetType().GetMethod("Q",
                    new[] { typeof(string), typeof(string) });
                object element = null;
                if (qMethod != null)
                {
                    element = qMethod.Invoke(root, new object[] { name, null });
                }

                if (element == null) continue;

                var elType = element.GetType();
                var data = new JObject
                {
                    ["name"] = GetStringProp(element, "name") ?? string.Empty,
                    ["type"] = elType.Name,
                    ["fullType"] = elType.FullName,
                    ["visible"] = GetBoolProp(element, "visible"),
                    ["enabledSelf"] = GetBoolProp(element, "enabledSelf"),
                    ["enabledInHierarchy"] = GetBoolProp(element, "enabledInHierarchy")
                };

                // Try to get value if it's a value-bearing element
                var valueProp = elType.GetProperty("value");
                if (valueProp != null)
                {
                    var val = valueProp.GetValue(element);
                    data["value"] = val?.ToString() ?? "null";
                    data["valueType"] = valueProp.PropertyType.Name;
                }

                // Get text if available
                var textProp = elType.GetProperty("text");
                if (textProp != null && textProp.PropertyType == typeof(string))
                {
                    data["text"] = textProp.GetValue(element) as string ?? string.Empty;
                }

                // Get class list
                var getClassesMethod = element.GetType().GetMethod("GetClasses");
                if (getClassesMethod != null)
                {
                    var classes = getClassesMethod.Invoke(element, null) as IEnumerable;
                    var classArr = new JArray();
                    if (classes != null)
                    {
                        foreach (var cls in classes)
                            classArr.Add(cls?.ToString() ?? string.Empty);
                    }
                    data["classes"] = classArr;
                }

                // Child count
                var childCountProp = elType.GetProperty("childCount");
                if (childCountProp != null)
                    data["childCount"] = (int)childCountProp.GetValue(element);

                return Ok($"UI Toolkit element '{name}'", data);
            }

            return Fail(StatusCode.NotFound, $"UI Toolkit element with name '{name}' not found.");
        }

        private static string GetStringProp(object obj, string propName)
        {
            var prop = obj.GetType().GetProperty(propName);
            return prop?.GetValue(obj) as string;
        }

        private static bool GetBoolProp(object obj, string propName)
        {
            var prop = obj.GetType().GetProperty(propName);
            if (prop != null && prop.PropertyType == typeof(bool))
                return (bool)prop.GetValue(obj);
            return true;
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
