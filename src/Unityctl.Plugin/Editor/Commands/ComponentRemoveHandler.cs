using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ComponentRemoveHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ComponentRemove;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var componentId = request.GetParam("componentId", null);
            if (string.IsNullOrEmpty(componentId))
                return InvalidParameters("Parameter 'componentId' is required.");

            var component = GlobalObjectIdResolver.Resolve<UnityEngine.Component>(componentId);
            if (component == null)
                return Fail(StatusCode.NotFound, $"Component not found: {componentId}");

            var prefabReject = PrefabGuard.RejectIfPrefab(component);
            if (prefabReject != null) return prefabReject;

            var goName = component.gameObject.name;
            var typeName = component.GetType().FullName;
            var scenePath = component.gameObject.scene.path;
            var undoName = $"unityctl: component-remove: {component.GetType().Name} from {goName}";

            using (new UndoScope(undoName))
            {
                UnityEditor.Undo.DestroyObjectImmediate(component);
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            return Ok($"Removed {typeName} from '{goName}'", new JObject
            {
                ["removedComponentType"] = typeName,
                ["gameObjectName"] = goName,
                ["scenePath"] = scenePath,
                ["sceneDirty"] = true,
                ["undoGroupName"] = undoName
            });
#else
            return NotInEditor();
#endif
        }
    }
}
