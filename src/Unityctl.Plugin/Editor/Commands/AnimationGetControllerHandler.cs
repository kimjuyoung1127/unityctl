#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AnimationGetControllerHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AnimationGetController;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
                return Fail(StatusCode.NotFound, $"AnimatorController not found at: {path}");

            var layers = new JArray();
            foreach (var layer in controller.layers)
            {
                var states = new JArray();
                if (layer.stateMachine != null)
                    CollectStates(layer.stateMachine, states, 0);

                layers.Add(new JObject
                {
                    ["name"] = layer.name,
                    ["defaultWeight"] = layer.defaultWeight,
                    ["blendingMode"] = layer.blendingMode.ToString(),
                    ["stateCount"] = states.Count,
                    ["states"] = states
                });
            }

            var parameters = new JArray();
            foreach (var param in controller.parameters)
            {
                parameters.Add(new JObject
                {
                    ["name"] = param.name,
                    ["type"] = param.type.ToString(),
                    ["defaultFloat"] = param.defaultFloat,
                    ["defaultInt"] = param.defaultInt,
                    ["defaultBool"] = param.defaultBool
                });
            }

            var data = new JObject
            {
                ["path"] = path,
                ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["name"] = controller.name,
                ["layerCount"] = controller.layers.Length,
                ["layers"] = layers,
                ["parameterCount"] = controller.parameters.Length,
                ["parameters"] = parameters
            };

            return Ok($"AnimatorController details for '{path}'", data);
        }

        private static void CollectStates(AnimatorStateMachine sm, JArray states, int depth)
        {
            if (depth > 3) return;

            foreach (var childState in sm.states)
            {
                var state = childState.state;
                var motionName = state.motion != null ? state.motion.name : null;

                states.Add(new JObject
                {
                    ["name"] = state.name,
                    ["motion"] = motionName,
                    ["speed"] = state.speed,
                    ["tag"] = state.tag,
                    ["transitionCount"] = state.transitions.Length
                });
            }

            foreach (var childSm in sm.stateMachines)
            {
                CollectStates(childSm.stateMachine, states, depth + 1);
            }
        }
    }
}
#endif
