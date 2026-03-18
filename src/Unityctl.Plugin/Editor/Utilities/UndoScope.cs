#if UNITY_EDITOR
using System;
using UnityEditor;

namespace Unityctl.Plugin.Editor.Utilities
{
    /// <summary>
    /// IDisposable Undo group management.
    /// Creates an undo group on construction, closes it on disposal.
    /// </summary>
    public sealed class UndoScope : IDisposable
    {
        private readonly int _group;

        public string GroupName { get; }

        public UndoScope(string groupName)
        {
            GroupName = groupName;
            Undo.IncrementCurrentGroup();
            _group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(groupName);
        }

        public void Dispose()
        {
            Undo.CollapseUndoOperations(_group);
        }
    }
}
#endif
