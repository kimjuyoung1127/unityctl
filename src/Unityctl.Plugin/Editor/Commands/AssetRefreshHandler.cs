using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    /// <summary>
    /// Triggers AssetDatabase.Refresh() asynchronously via delayCall.
    /// Returns Ready immediately once refresh is successfully scheduled.
    /// IPC-only — batch mode cannot guarantee execution after response.
    /// </summary>
    public class AssetRefreshHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetRefresh;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            if (UnityEngine.Application.isBatchMode)
            {
                return Fail(StatusCode.InvalidParameters,
                    "asset-refresh is IPC-only. Batch mode cannot guarantee execution after response.");
            }

            var requestId = request.requestId;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                try
                {
                    // Defer the actual refresh one more editor tick so the Accepted response
                    // can flush before domain reload side effects begin.
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            UnityEditor.AssetDatabase.Refresh();
                        }
                        catch (System.Exception e)
                        {
                            UnityEngine.Debug.LogError($"unityctl asset-refresh delayed execution failed: {e}");
                        }
                    };
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"unityctl asset-refresh scheduling failed: {e}");
                }
            };

            return Ok("Asset refresh scheduled", new JObject
            {
                ["requestId"] = requestId,
                ["status"] = "scheduled"
            });
#else
            return NotInEditor();
#endif
        }
    }
}
