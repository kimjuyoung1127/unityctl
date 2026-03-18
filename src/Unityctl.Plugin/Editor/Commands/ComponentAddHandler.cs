using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ComponentAddHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ComponentAdd;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var goId = request.GetParam("id", null);
            var typeName = request.GetParam("type", null);

            if (string.IsNullOrEmpty(goId))
                return InvalidParameters("Parameter 'id' is required.");
            if (string.IsNullOrEmpty(typeName))
                return InvalidParameters("Parameter 'type' is required.");

            var go = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(goId);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {goId}");

            var prefabReject = PrefabGuard.RejectIfPrefab(go);
            if (prefabReject != null) return prefabReject;

            // Resolve runtime Type via TypeCache
            var type = UnityEditor.TypeCache.GetTypesDerivedFrom<UnityEngine.Component>()
                .FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);

            if (type == null)
                return InvalidParameters($"Component type not found: {typeName}");

            var undoName = $"unityctl: component-add: {type.Name} on {go.name}";
            using (new UndoScope(undoName))
            {
                // ObjectFactory.AddComponent has built-in Undo
                var component = UnityEditor.ObjectFactory.AddComponent(go, type);
                if (component == null)
                    return Fail(StatusCode.UnknownError, $"Failed to add component: {type.Name}");

                EditorSceneManager.MarkSceneDirty(go.scene);

                var componentId = GlobalObjectIdResolver.GetId(component);
                return Ok($"Added {type.Name} to '{go.name}'", new JObject
                {
                    ["globalObjectId"] = goId,
                    ["componentGlobalObjectId"] = componentId,
                    ["componentType"] = type.FullName,
                    ["gameObjectName"] = go.name,
                    ["scenePath"] = go.scene.path,
                    ["sceneDirty"] = true,
                    ["undoGroupName"] = undoName
                });
            }
#else
            return NotInEditor();
#endif
        }
    }
}
