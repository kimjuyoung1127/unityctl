#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Unityctl.Plugin.Editor.Utilities
{
    /// <summary>
    /// Resolves GlobalObjectId strings to Unity Objects.
    /// Primary key for all write operations.
    /// </summary>
    public static class GlobalObjectIdResolver
    {
        /// <summary>
        /// Resolve a GlobalObjectId string to a specific Unity Object type.
        /// Returns null if parsing fails or the object is not found/not the expected type.
        /// </summary>
        public static T Resolve<T>(string globalObjectIdString) where T : Object
        {
            if (string.IsNullOrEmpty(globalObjectIdString))
                return null;

            if (!GlobalObjectId.TryParse(globalObjectIdString, out var gid))
                return null;

            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            return obj as T;
        }

        /// <summary>
        /// Resolve a GlobalObjectId string to any Unity Object.
        /// </summary>
        public static Object Resolve(string globalObjectIdString)
        {
            return Resolve<Object>(globalObjectIdString);
        }

        /// <summary>
        /// Get the GlobalObjectId string for a Unity Object.
        /// </summary>
        public static string GetId(Object obj)
        {
            if (obj == null) return null;
            var gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            return gid.ToString();
        }
    }
}
#endif
