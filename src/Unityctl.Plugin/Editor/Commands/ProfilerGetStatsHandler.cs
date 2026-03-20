#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ProfilerGetStatsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ProfilerGetStats;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var isPlaying = EditorApplication.isPlaying;

            var data = new JObject
            {
                ["isPlaying"] = isPlaying,
                ["profilerEnabled"] = Profiler.enabled,
                ["totalAllocatedMemoryMB"] = Profiler.GetTotalAllocatedMemoryLong() / (1024.0 * 1024.0),
                ["totalReservedMemoryMB"] = Profiler.GetTotalReservedMemoryLong() / (1024.0 * 1024.0),
                ["totalUnusedReservedMemoryMB"] = Profiler.GetTotalUnusedReservedMemoryLong() / (1024.0 * 1024.0),
                ["monoUsedSizeMB"] = Profiler.GetMonoUsedSizeLong() / (1024.0 * 1024.0),
                ["monoHeapSizeMB"] = Profiler.GetMonoHeapSizeLong() / (1024.0 * 1024.0)
            };

            string message;
            if (isPlaying)
            {
                message = "Profiler stats (Play Mode — all stats valid)";
            }
            else
            {
                data["message"] = "Editor Mode: only memory stats are valid. Enter Play Mode for rendering stats.";
                message = "Profiler stats (Editor Mode — memory only)";
            }

            return Ok(message, data);
        }
    }
}
#endif
