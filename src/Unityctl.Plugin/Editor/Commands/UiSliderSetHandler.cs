#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UiSliderSetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UiSliderSet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var valueStr = request.GetParam("value", null);
            if (string.IsNullOrEmpty(valueStr))
                return InvalidParameters("Parameter 'value' is required.");

            if (!float.TryParse(valueStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var targetValue))
                return InvalidParameters("Parameter 'value' must be a valid number.");

            var requestedMode = request.GetParam("mode", "auto");
            if (!UiInteractionCommandHelper.TryResolveMode(requestedMode, out var effectiveMode, out var modeFailure))
                return modeFailure;

            if (!UiInteractionCommandHelper.TryResolveUiComponent<UnityEngine.UI.Slider>(id, "Slider", out var slider, out var failure))
                return failure;

            var previousValue = slider.value;

            if (effectiveMode == "play")
            {
                slider.SetValueWithoutNotify(targetValue);
            }
            else
            {
                var prefabReject = PrefabGuard.RejectIfPrefab(slider);
                if (prefabReject != null) return prefabReject;

                var undoName = $"unityctl: ui-slider-set: {slider.gameObject.name}";
                using (new UndoScope(undoName))
                {
                    Undo.RecordObject(slider, undoName);
                    slider.SetValueWithoutNotify(targetValue);
                    EditorUtility.SetDirty(slider);
                    EditorSceneManager.MarkSceneDirty(slider.gameObject.scene);
                }
            }

            return Ok($"Slider '{slider.gameObject.name}' set to {slider.value}", new JObject
            {
                ["globalObjectId"] = id,
                ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(slider),
                ["gameObjectName"] = slider.gameObject.name,
                ["uiType"] = "Slider",
                ["requestedMode"] = requestedMode,
                ["modeApplied"] = effectiveMode,
                ["previousValue"] = previousValue,
                ["currentValue"] = slider.value,
                ["minValue"] = slider.minValue,
                ["maxValue"] = slider.maxValue,
                ["eventsTriggered"] = false,
                ["scenePath"] = slider.gameObject.scene.path,
                ["sceneDirty"] = effectiveMode != "play"
            });
        }
    }
}
#endif
