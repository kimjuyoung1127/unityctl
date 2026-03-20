#if UNITY_EDITOR
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UitkSetValueHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UitkSetValue;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var uiDocType = FindType("UnityEngine.UIElements.UIDocument");
            if (uiDocType == null)
                return Fail(StatusCode.NotFound, "UI Toolkit (UIDocument) not available in this Unity version.");

            var name = request.GetParam("name", null);
            if (string.IsNullOrEmpty(name))
                return InvalidParameters("Parameter 'name' is required.");

            var valueStr = request.GetParam("value", null);
            if (valueStr == null)
                return InvalidParameters("Parameter 'value' is required.");

            var docs = UnityEngine.Object.FindObjectsByType(uiDocType,
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var doc in docs)
            {
                var rootProp = doc.GetType().GetProperty("rootVisualElement");
                var root = rootProp?.GetValue(doc);
                if (root == null) continue;

                var qMethod = root.GetType().GetMethod("Q",
                    new[] { typeof(string), typeof(string) });
                object element = null;
                if (qMethod != null)
                    element = qMethod.Invoke(root, new object[] { name, null });

                if (element == null) continue;

                var elType = element.GetType();
                var valueProp = elType.GetProperty("value");
                if (valueProp == null || !valueProp.CanWrite)
                    return Fail(StatusCode.InvalidParameters,
                        $"Element '{name}' ({elType.Name}) does not have a writable value property.");

                object parsedValue;
                try
                {
                    parsedValue = ConvertValue(valueStr, valueProp.PropertyType);
                }
                catch (Exception ex)
                {
                    return InvalidParameters($"Cannot convert '{valueStr}' to {valueProp.PropertyType.Name}: {ex.Message}");
                }

                var previousValue = valueProp.GetValue(element)?.ToString() ?? "null";

                // Use SetValueWithoutNotify if available, otherwise direct set
                var setWithoutNotify = elType.GetMethod("SetValueWithoutNotify");
                if (setWithoutNotify != null)
                {
                    try
                    {
                        setWithoutNotify.Invoke(element, new[] { parsedValue });
                    }
                    catch
                    {
                        valueProp.SetValue(element, parsedValue);
                    }
                }
                else
                {
                    valueProp.SetValue(element, parsedValue);
                }

                return Ok($"Set '{name}' value to '{valueStr}'", new JObject
                {
                    ["name"] = name,
                    ["type"] = elType.Name,
                    ["previousValue"] = previousValue,
                    ["currentValue"] = valueProp.GetValue(element)?.ToString() ?? "null"
                });
            }

            return Fail(StatusCode.NotFound, $"UI Toolkit element with name '{name}' not found.");
        }

        private static object ConvertValue(string str, Type targetType)
        {
            if (targetType == typeof(string)) return str;
            if (targetType == typeof(float)) return float.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(int)) return int.Parse(str);
            if (targetType == typeof(bool)) return bool.Parse(str);
            if (targetType == typeof(double)) return double.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
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
