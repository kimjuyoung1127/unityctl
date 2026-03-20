#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class VolumeGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.VolumeGet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var volumeType = FindType("UnityEngine.Rendering.Volume");
            if (volumeType == null)
                return Fail(StatusCode.NotFound,
                    "Volume API not available. Install com.unity.render-pipelines.universal or .high-definition");

            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var component = GlobalObjectIdResolver.Resolve(id);
            if (component == null)
                return Fail(StatusCode.NotFound, $"Object not found for id: {id}");

            Component volume = null;
            if (volumeType.IsInstanceOfType(component))
            {
                volume = component as Component;
            }
            else if (component is GameObject go)
            {
                volume = go.GetComponent(volumeType) as Component;
            }

            if (volume == null)
                return Fail(StatusCode.NotFound, $"No Volume component found on: {id}");

            var isGlobal = GetProperty<bool>(volume, "isGlobal");
            var priority = GetProperty<float>(volume, "priority");
            var profileProp = volume.GetType().GetProperty("profile");
            var profile = profileProp?.GetValue(volume);

            var data = new JObject
            {
                ["globalObjectId"] = GlobalObjectIdResolver.GetId(volume),
                ["gameObjectName"] = volume.gameObject.name,
                ["isGlobal"] = isGlobal,
                ["priority"] = priority
            };

            if (profile != null)
            {
                var componentsProp = profile.GetType().GetProperty("components");
                var components = componentsProp?.GetValue(profile) as IList;
                var overrides = new JArray();
                if (components != null)
                {
                    foreach (var vc in components)
                    {
                        var activeProp = vc.GetType().GetProperty("active");
                        var isActive = activeProp != null && (bool)activeProp.GetValue(vc);
                        overrides.Add(new JObject
                        {
                            ["type"] = vc.GetType().Name,
                            ["fullType"] = vc.GetType().FullName,
                            ["active"] = isActive
                        });
                    }
                }
                data["overrideCount"] = overrides.Count;
                data["overrides"] = overrides;
            }

            return Ok($"Volume details for '{volume.gameObject.name}'", data);
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

        private static T GetProperty<T>(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name);
            if (prop != null) return (T)prop.GetValue(obj);
            return default;
        }
    }
}
#endif
