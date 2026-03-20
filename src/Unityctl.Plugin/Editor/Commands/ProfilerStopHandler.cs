#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEngine.Profiling;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ProfilerStopHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ProfilerStop;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var wasEnabled = Profiler.enabled;
            Profiler.enabled = false;

            return Ok(wasEnabled ? "Profiler disabled" : "Profiler was already disabled", new JObject
            {
                ["profilerEnabled"] = false,
                ["wasEnabled"] = wasEnabled
            });
        }
    }
}
#endif
