using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ComponentGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ComponentGet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var componentId = request.GetParam("componentId", null);
            var property = request.GetParam("property", null);
            var full = request.GetParam<bool>("full");

            if (string.IsNullOrEmpty(componentId))
            {
                return InvalidParameters("Parameter 'componentId' is required.");
            }

            var component = GlobalObjectIdResolver.Resolve<UnityEngine.Component>(componentId);
            if (component == null)
            {
                return Fail(StatusCode.NotFound, $"Component not found: {componentId}");
            }

            var data = new JObject
            {
                ["componentGlobalObjectId"] = componentId,
                ["gameObjectGlobalObjectId"] = GlobalObjectIdResolver.GetId(component.gameObject),
                ["gameObjectName"] = component.gameObject.name,
                ["typeName"] = component.GetType().FullName ?? component.GetType().Name,
                ["enabled"] = component is UnityEngine.Behaviour behaviour ? behaviour.enabled : true,
                ["scenePath"] = SceneExplorationUtility.GetHierarchyPath(component.gameObject),
                ["sceneAssetPath"] = component.gameObject.scene.path
            };

            if (!string.IsNullOrWhiteSpace(property))
            {
                using (var serializedObject = new SerializedObject(component))
                {
                    var serializedProperty = serializedObject.FindProperty(property);
                    if (serializedProperty == null)
                    {
                        return Fail(StatusCode.NotFound, $"Property '{property}' not found on component '{componentId}'.");
                    }

                    data["property"] = property;
                    data["propertyType"] = serializedProperty.propertyType.ToString();
                    data["value"] = SerializedPropertyJsonUtility.ToJsonValue(serializedProperty);
                }

                return Ok($"Component property '{property}'", data);
            }

            if (full)
            {
                data["properties"] = SerializedPropertyJsonUtility.GetVisibleProperties(component);
            }
            else
            {
                // Summary mode: property count + key field names only
                using (var so = new SerializedObject(component))
                {
                    var iter = so.GetIterator();
                    var names = new JArray();
                    int count = 0;
                    if (iter.NextVisible(true))
                    {
                        do
                        {
                            names.Add(iter.name);
                            count++;
                        }
                        while (iter.NextVisible(false));
                    }
                    data["propertyCount"] = count;
                    data["propertyNames"] = names;
                }
            }
            return Ok($"Component '{component.GetType().Name}'", data);
#else
            return NotInEditor();
#endif
        }
    }
}
