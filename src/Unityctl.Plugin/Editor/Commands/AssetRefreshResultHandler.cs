using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    /// <summary>
    /// Polls the result of a previously accepted asset-refresh operation.
    /// </summary>
    public class AssetRefreshResultHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetRefreshResult;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var requestId = request.GetParam("requestId", null);
            if (string.IsNullOrEmpty(requestId))
                return InvalidParameters("Parameter 'requestId' is required.");

            var state = AsyncOperationRegistry.TryGet(requestId);
            if (state == null)
                return Fail(StatusCode.NotFound, $"No operation found for requestId: {requestId}");

            if (state.Status == AsyncStatus.Running)
            {
                return Ok(StatusCode.Accepted, "Asset refresh still in progress", new JObject
                {
                    ["requestId"] = requestId,
                    ["status"] = "running"
                });
            }

            // Completed
            return state.Response ?? Ok("Asset refresh completed", new JObject
            {
                ["requestId"] = requestId,
                ["status"] = "completed"
            });
#else
            return NotInEditor();
#endif
        }
    }
}
