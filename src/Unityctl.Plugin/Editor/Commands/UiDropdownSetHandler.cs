#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UiDropdownSetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UiDropdownSet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var valueStr = request.GetParam("value", null);
            if (string.IsNullOrEmpty(valueStr))
                return InvalidParameters("Parameter 'value' is required.");

            if (!int.TryParse(valueStr, out var targetValue))
                return InvalidParameters("Parameter 'value' must be a valid integer.");

            var requestedMode = request.GetParam("mode", "auto");
            if (!UiInteractionCommandHelper.TryResolveMode(requestedMode, out var effectiveMode, out var modeFailure))
                return modeFailure;

            if (!UiInteractionCommandHelper.TryResolveUiComponent<UnityEngine.UI.Dropdown>(id, "Dropdown", out var dropdown, out var failure))
                return failure;

            if (targetValue < 0 || targetValue >= dropdown.options.Count)
                return InvalidParameters($"Parameter 'value' must be between 0 and {dropdown.options.Count - 1} (inclusive). Got: {targetValue}");

            var previousValue = dropdown.value;
            var previousText = dropdown.options[previousValue].text;
            var targetText = dropdown.options[targetValue].text;

            if (effectiveMode == "play")
            {
                dropdown.SetValueWithoutNotify(targetValue);
            }
            else
            {
                var prefabReject = PrefabGuard.RejectIfPrefab(dropdown);
                if (prefabReject != null) return prefabReject;

                var undoName = $"unityctl: ui-dropdown-set: {dropdown.gameObject.name}";
                using (new UndoScope(undoName))
                {
                    Undo.RecordObject(dropdown, undoName);
                    dropdown.SetValueWithoutNotify(targetValue);
                    EditorUtility.SetDirty(dropdown);
                    EditorSceneManager.MarkSceneDirty(dropdown.gameObject.scene);
                }
            }

            return Ok($"Dropdown '{dropdown.gameObject.name}' set to index {dropdown.value} ('{targetText}')", new JObject
            {
                ["globalObjectId"] = id,
                ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(dropdown),
                ["gameObjectName"] = dropdown.gameObject.name,
                ["uiType"] = "Dropdown",
                ["requestedMode"] = requestedMode,
                ["modeApplied"] = effectiveMode,
                ["previousValue"] = previousValue,
                ["previousText"] = previousText,
                ["currentValue"] = dropdown.value,
                ["currentText"] = targetText,
                ["optionCount"] = dropdown.options.Count,
                ["eventsTriggered"] = false,
                ["scenePath"] = dropdown.gameObject.scene.path,
                ["sceneDirty"] = effectiveMode != "play"
            });
        }
    }
}
#endif
