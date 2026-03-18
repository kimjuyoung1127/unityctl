using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class SceneSnapshotHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.SceneSnapshot;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var scenePath = request.GetParam("scenePath", null);
            var includeInactive = request.GetParam<bool>("includeInactive");

            // Objects to look up in batch + the JObject entries to receive the id
            var allObjects = new List<UnityEngine.Object>();
            var pendingIds = new List<(JObject entry, string field)>();

            var sceneSetup = new JArray();
            var scenes = new JArray();

            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            // Pass 1: record sceneSetup (all scenes, loaded or not)
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                sceneSetup.Add(new JObject
                {
                    ["path"] = scene.path,
                    ["isLoaded"] = scene.isLoaded,
                    ["isActive"] = scene.path == activeScene.path
                });
            }

            // Pass 2: collect game objects from loaded scenes
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                if (scenePath != null && scene.path != scenePath) continue;

                var gameObjectsArray = new JArray();
                var rootObjects = scene.GetRootGameObjects();

                foreach (var root in rootObjects)
                {
                    TraverseGameObject(root, string.Empty, gameObjectsArray,
                        allObjects, pendingIds, includeInactive);
                }

                scenes.Add(new JObject
                {
                    ["path"] = scene.path,
                    ["name"] = scene.name,
                    ["isDirty"] = scene.isDirty,
                    ["gameObjects"] = gameObjectsArray
                });
            }

            // Batch GlobalObjectId lookup (one call instead of per-object)
            if (allObjects.Count > 0)
            {
                var ids = new UnityEditor.GlobalObjectId[allObjects.Count];
                UnityEditor.GlobalObjectId.GetGlobalObjectIdsSlow(allObjects.ToArray(), ids);

                for (int i = 0; i < pendingIds.Count; i++)
                {
                    var (entry, field) = pendingIds[i];
                    entry[field] = ids[i].ToString();
                }
            }

            var data = new JObject
            {
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["unityVersion"] = UnityEngine.Application.unityVersion,
                ["projectPath"] = UnityEngine.Application.dataPath.Replace("/Assets", string.Empty),
                ["sceneSetup"] = sceneSetup,
                ["scenes"] = scenes
            };

            return Ok("Scene snapshot captured", data);
#else
            return NotInEditor();
#endif
        }

        protected override CommandResponse HandleException(Exception exception)
        {
            return Fail(StatusCode.UnknownError, $"Scene snapshot failed: {exception.Message}",
                errors: GetStackTrace(exception));
        }

#if UNITY_EDITOR
        private static void TraverseGameObject(
            UnityEngine.GameObject go,
            string parentPath,
            JArray output,
            List<UnityEngine.Object> allObjects,
            List<(JObject, string)> pendingIds,
            bool includeInactive)
        {
            if (!includeInactive && !go.activeSelf) return;

            var scenePath = string.IsNullOrEmpty(parentPath) ? go.name : parentPath + "/" + go.name;
            var components = go.GetComponents<UnityEngine.Component>();

            var componentsArray = new JArray();
            foreach (var comp in components)
            {
                if (comp == null) continue; // Missing script guard

                var properties = new JObject();
                using (var so = new UnityEditor.SerializedObject(comp))
                {
                    var iter = so.GetIterator();
                    while (iter.NextVisible(true))
                    {
                        var propPath = iter.propertyPath;
                        var value = SerializedPropertyToValue(iter);
                        if (value != null)
                            properties[propPath] = value;
                    }
                }

                var compEntry = new JObject
                {
                    ["globalObjectId"] = string.Empty,
                    ["typeName"] = comp.GetType().FullName ?? comp.GetType().Name,
                    ["enabled"] = comp is UnityEngine.Behaviour beh ? beh.enabled : true,
                    ["properties"] = properties
                };

                componentsArray.Add(compEntry);
                pendingIds.Add((compEntry, "globalObjectId"));
                allObjects.Add(comp);
            }

            var goEntry = new JObject
            {
                ["globalObjectId"] = string.Empty,
                ["name"] = go.name,
                ["activeSelf"] = go.activeSelf,
                ["layer"] = go.layer,
                ["tag"] = go.tag,
                ["scenePath"] = scenePath,
                ["components"] = componentsArray
            };

            output.Add(goEntry);
            pendingIds.Add((goEntry, "globalObjectId"));
            allObjects.Add(go);

            // Recurse into children
            for (int i = 0; i < go.transform.childCount; i++)
            {
                TraverseGameObject(
                    go.transform.GetChild(i).gameObject,
                    scenePath,
                    output,
                    allObjects,
                    pendingIds,
                    includeInactive);
            }
        }

        private static JToken SerializedPropertyToValue(UnityEditor.SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case UnityEditor.SerializedPropertyType.Integer:
                    return prop.intValue;
                case UnityEditor.SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case UnityEditor.SerializedPropertyType.Float:
                    return prop.floatValue;
                case UnityEditor.SerializedPropertyType.String:
                    return prop.stringValue ?? string.Empty;
                case UnityEditor.SerializedPropertyType.Color:
                    return new JObject
                    {
                        ["r"] = prop.colorValue.r,
                        ["g"] = prop.colorValue.g,
                        ["b"] = prop.colorValue.b,
                        ["a"] = prop.colorValue.a
                    };
                case UnityEditor.SerializedPropertyType.Vector2:
                    return new JObject { ["x"] = prop.vector2Value.x, ["y"] = prop.vector2Value.y };
                case UnityEditor.SerializedPropertyType.Vector3:
                    return new JObject
                    {
                        ["x"] = prop.vector3Value.x,
                        ["y"] = prop.vector3Value.y,
                        ["z"] = prop.vector3Value.z
                    };
                case UnityEditor.SerializedPropertyType.Vector4:
                    return new JObject
                    {
                        ["x"] = prop.vector4Value.x,
                        ["y"] = prop.vector4Value.y,
                        ["z"] = prop.vector4Value.z,
                        ["w"] = prop.vector4Value.w
                    };
                case UnityEditor.SerializedPropertyType.Quaternion:
                    return new JObject
                    {
                        ["x"] = prop.quaternionValue.x,
                        ["y"] = prop.quaternionValue.y,
                        ["z"] = prop.quaternionValue.z,
                        ["w"] = prop.quaternionValue.w
                    };
                case UnityEditor.SerializedPropertyType.Enum:
                    return prop.enumValueIndex;
                case UnityEditor.SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue != null
                        ? (JToken)prop.objectReferenceValue.name
                        : JValue.CreateNull();
                case UnityEditor.SerializedPropertyType.LayerMask:
                    return prop.intValue;
                case UnityEditor.SerializedPropertyType.ArraySize:
                    return prop.intValue;
                case UnityEditor.SerializedPropertyType.Rect:
                    return new JObject
                    {
                        ["x"] = prop.rectValue.x,
                        ["y"] = prop.rectValue.y,
                        ["width"] = prop.rectValue.width,
                        ["height"] = prop.rectValue.height
                    };
                case UnityEditor.SerializedPropertyType.Bounds:
                    return new JObject
                    {
                        ["centerX"] = prop.boundsValue.center.x,
                        ["centerY"] = prop.boundsValue.center.y,
                        ["centerZ"] = prop.boundsValue.center.z,
                        ["extentsX"] = prop.boundsValue.extents.x,
                        ["extentsY"] = prop.boundsValue.extents.y,
                        ["extentsZ"] = prop.boundsValue.extents.z
                    };
                default:
                    return prop.ToString();
            }
        }
#endif
    }
}
