#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UiScrollHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UiScroll;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var xStr = request.GetParam("x", null);
            var yStr = request.GetParam("y", null);
            if (string.IsNullOrEmpty(xStr) && string.IsNullOrEmpty(yStr))
                return InvalidParameters("At least one of 'x' or 'y' must be provided.");

            var requestedMode = request.GetParam("mode", "auto");
            if (!UiInteractionCommandHelper.TryResolveMode(requestedMode, out var effectiveMode, out var modeFailure))
                return modeFailure;

            if (!UiInteractionCommandHelper.TryResolveUiComponent<UnityEngine.UI.ScrollRect>(id, "ScrollRect", out var scrollRect, out var failure))
                return failure;

            var prev = scrollRect.normalizedPosition;
            float x = !string.IsNullOrEmpty(xStr) && float.TryParse(xStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var px) ? px : prev.x;
            float y = !string.IsNullOrEmpty(yStr) && float.TryParse(yStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var py) ? py : prev.y;

            if (effectiveMode == "play")
            {
                scrollRect.normalizedPosition = new UnityEngine.Vector2(x, y);
            }
            else
            {
                var prefabReject = PrefabGuard.RejectIfPrefab(scrollRect);
                if (prefabReject != null) return prefabReject;

                var undoName = $"unityctl: ui-scroll: {scrollRect.gameObject.name}";
                using (new UndoScope(undoName))
                {
                    Undo.RecordObject(scrollRect, undoName);
                    scrollRect.normalizedPosition = new UnityEngine.Vector2(x, y);
                    EditorUtility.SetDirty(scrollRect);
                    EditorSceneManager.MarkSceneDirty(scrollRect.gameObject.scene);
                }
            }

            return Ok($"ScrollRect '{scrollRect.gameObject.name}' scrolled to ({x:F3}, {y:F3})", new JObject
            {
                ["globalObjectId"] = id,
                ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(scrollRect),
                ["gameObjectName"] = scrollRect.gameObject.name,
                ["uiType"] = "ScrollRect",
                ["requestedMode"] = requestedMode,
                ["modeApplied"] = effectiveMode,
                ["previousX"] = prev.x,
                ["previousY"] = prev.y,
                ["currentX"] = x,
                ["currentY"] = y,
                ["scenePath"] = scrollRect.gameObject.scene.path,
                ["sceneDirty"] = effectiveMode != "play"
            });
        }
    }
}
#endif
