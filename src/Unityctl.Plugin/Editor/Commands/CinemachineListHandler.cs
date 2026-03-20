#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class CinemachineListHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.CinemachineList;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            // Try Cinemachine 3.x first, then 2.x
            var vcamType = FindType("Unity.Cinemachine.CinemachineCamera")
                        ?? FindType("Cinemachine.CinemachineVirtualCamera");

            if (vcamType == null)
                return Fail(StatusCode.NotFound,
                    "Cinemachine not available. Install com.unity.cinemachine package.");

            var includeInactive = request.GetParam("includeInactive", false);
            var vcams = UnityEngine.Object.FindObjectsByType(vcamType, includeInactive
                ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            var results = new JArray();
            foreach (var vcam in vcams)
            {
                var comp = vcam as Component;
                if (comp == null) continue;

                var go = comp.gameObject;
                var priority = GetPropertyValue<int>(vcam, "Priority");

                results.Add(new JObject
                {
                    ["globalObjectId"] = GlobalObjectIdResolver.GetId(comp),
                    ["gameObjectName"] = go.name,
                    ["gameObjectId"] = GlobalObjectIdResolver.GetId(go),
                    ["type"] = vcam.GetType().Name,
                    ["priority"] = priority,
                    ["isActive"] = go.activeInHierarchy,
                    ["scenePath"] = go.scene.path
                });
            }

            return Ok($"Found {results.Count} Cinemachine camera(s)", new JObject
            {
                ["results"] = results,
                ["cinemachineVersion"] = vcamType.Namespace == "Unity.Cinemachine" ? "3.x" : "2.x"
            });
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

        private static T GetPropertyValue<T>(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name);
            if (prop != null) return (T)prop.GetValue(obj);
            return default;
        }
    }
}
#endif
