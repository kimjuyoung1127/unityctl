#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Utilities
{
    /// <summary>
    /// Guards write operations against prefab instances.
    /// v1 does not support writing to prefab instances — scene objects only.
    /// </summary>
    public static class PrefabGuard
    {
        private const string RejectionMessage =
            "Write operations on prefab instances are not supported in v1. " +
            "Use scene objects or unpack the prefab first.";

        /// <summary>
        /// Returns a failure CommandResponse if the target is part of a prefab instance.
        /// Returns null if it's safe to proceed (scene object).
        /// </summary>
        public static CommandResponse RejectIfPrefab(Object target)
        {
            if (target == null) return null;

            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                return CommandResponse.Fail(StatusCode.InvalidParameters, RejectionMessage);
            }

            return null;
        }
    }
}
#endif
