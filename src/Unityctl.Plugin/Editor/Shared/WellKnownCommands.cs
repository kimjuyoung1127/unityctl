namespace Unityctl.Plugin.Editor.Shared
{
    public static class WellKnownCommands
    {
        public const string Ping = "ping";
        public const string Status = "status";
        public const string Build = "build";
        public const string Test = "test";
        public const string Check = "check";
        public const string TestResult = "test-result";
        public const string Watch = "watch";
        public const string SceneSnapshot = "scene-snapshot";
        public const string SceneDiff = "scene-diff";
        public const string Exec = "exec";

        // Write API — Phase A
        public const string PlayMode = "play-mode";
        public const string PlayerSettings = "player-settings";
        public const string AssetRefresh = "asset-refresh";
        public const string AssetRefreshResult = "asset-refresh-result";

        // Write API — Phase B
        public const string GameObjectCreate = "gameobject-create";
        public const string GameObjectDelete = "gameobject-delete";
        public const string GameObjectSetActive = "gameobject-set-active";
        public const string GameObjectMove = "gameobject-move";
        public const string GameObjectRename = "gameobject-rename";
        public const string SceneSave = "scene-save";

        // Write API — Phase B.5
        public const string ComponentAdd = "component-add";
        public const string ComponentRemove = "component-remove";
        public const string ComponentSetProperty = "component-set-property";
    }
}
