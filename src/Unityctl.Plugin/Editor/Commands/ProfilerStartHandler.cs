#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEngine.Profiling;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ProfilerStartHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ProfilerStart;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var wasEnabled = Profiler.enabled;
            Profiler.enabled = true;

            return Ok(wasEnabled ? "Profiler was already enabled" : "Profiler enabled", new JObject
            {
                ["profilerEnabled"] = true,
                ["wasAlreadyEnabled"] = wasEnabled
            });
        }
    }
}
#endif
