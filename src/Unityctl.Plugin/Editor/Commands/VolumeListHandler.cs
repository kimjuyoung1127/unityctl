#if UNITY_EDITOR
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class VolumeListHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.VolumeList;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var volumeType = FindType("UnityEngine.Rendering.Volume");
            if (volumeType == null)
                return Fail(StatusCode.NotFound,
                    "Volume API not available. Install com.unity.render-pipelines.universal or .high-definition");

            var includeInactive = request.GetParam("includeInactive", false);
            var volumes = UnityEngine.Object.FindObjectsByType(volumeType, includeInactive
                ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            var results = new JArray();
            foreach (var vol in volumes)
            {
                var comp = vol as Component;
                if (comp == null) continue;

                var go = comp.gameObject;
                var isGlobal = GetBoolProperty(vol, "isGlobal");
                var priority = GetFloatProperty(vol, "priority");

                results.Add(new JObject
                {
                    ["globalObjectId"] = GlobalObjectIdResolver.GetId(comp),
                    ["gameObjectName"] = go.name,
                    ["gameObjectId"] = GlobalObjectIdResolver.GetId(go),
                    ["isGlobal"] = isGlobal,
                    ["priority"] = priority,
                    ["isActive"] = go.activeInHierarchy,
                    ["scenePath"] = go.scene.path
                });
            }

            return Ok($"Found {results.Count} Volume(s)", new JObject
            {
                ["results"] = results
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

        private static bool GetBoolProperty(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name);
            if (prop != null) return (bool)prop.GetValue(obj);
            return false;
        }

        private static float GetFloatProperty(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name);
            if (prop != null) return (float)prop.GetValue(obj);
            return 0f;
        }
    }
}
#endif
