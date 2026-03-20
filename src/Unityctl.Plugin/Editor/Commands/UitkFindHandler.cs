#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UitkFindHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UitkFind;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var uiDocType = FindType("UnityEngine.UIElements.UIDocument");
            if (uiDocType == null)
                return Fail(StatusCode.NotFound, "UI Toolkit (UIDocument) not available in this Unity version.");

            var nameFilter = request.GetParam("name", null);
            var classNameFilter = request.GetParam("className", null);
            var typeFilter = request.GetParam("type", null);
            var limit = request.GetParam<int>("limit");

            var docs = UnityEngine.Object.FindObjectsByType(uiDocType,
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            var results = new JArray();
            foreach (var doc in docs)
            {
                var comp = doc as Component;
                if (comp == null) continue;

                var rootProp = doc.GetType().GetProperty("rootVisualElement");
                var root = rootProp?.GetValue(doc);
                if (root == null) continue;

                CollectElements(root, nameFilter, classNameFilter, typeFilter, results, limit, 0);

                if (limit > 0 && results.Count >= limit)
                    break;
            }

            return Ok($"Found {results.Count} UI Toolkit element(s)", new JObject
            {
                ["results"] = results
            });
        }

        private static void CollectElements(object element, string nameFilter, string classFilter,
            string typeFilter, JArray results, int limit, int depth)
        {
            if (depth > 20) return;
            if (limit > 0 && results.Count >= limit) return;

            var elType = element.GetType();
            var elName = GetStringProp(element, "name");
            var elTypeName = elType.Name;

            bool matches = true;
            if (!string.IsNullOrEmpty(nameFilter) &&
                (string.IsNullOrEmpty(elName) || elName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0))
                matches = false;

            if (matches && !string.IsNullOrEmpty(typeFilter) &&
                !elTypeName.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                matches = false;

            if (matches && !string.IsNullOrEmpty(classFilter))
            {
                var classList = GetClassList(element);
                if (classList == null || !ContainsClass(classList, classFilter))
                    matches = false;
            }

            if (matches)
            {
                results.Add(new JObject
                {
                    ["name"] = elName ?? string.Empty,
                    ["type"] = elTypeName,
                    ["fullType"] = elType.FullName,
                    ["visible"] = GetBoolProp(element, "visible"),
                    ["enabledSelf"] = GetBoolProp(element, "enabledSelf")
                });
            }

            // Recurse into children via hierarchy
            var childCountProp = elType.GetProperty("childCount");
            if (childCountProp == null) return;
            var childCount = (int)childCountProp.GetValue(element);

            var indexer = elType.GetMethod("ElementAt") ?? elType.GetMethod("get_Item");
            if (indexer == null) return;

            for (int i = 0; i < childCount; i++)
            {
                if (limit > 0 && results.Count >= limit) return;
                var child = indexer.Invoke(element, new object[] { i });
                if (child != null)
                    CollectElements(child, nameFilter, classFilter, typeFilter, results, limit, depth + 1);
            }
        }

        private static string GetStringProp(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name);
            return prop?.GetValue(obj) as string;
        }

        private static bool GetBoolProp(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name);
            if (prop != null && prop.PropertyType == typeof(bool))
                return (bool)prop.GetValue(obj);
            return true;
        }

        private static IEnumerable GetClassList(object element)
        {
            var method = element.GetType().GetMethod("GetClasses");
            return method?.Invoke(element, null) as IEnumerable;
        }

        private static bool ContainsClass(IEnumerable classList, string className)
        {
            foreach (var cls in classList)
            {
                if (cls is string s && s.Equals(className, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
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
