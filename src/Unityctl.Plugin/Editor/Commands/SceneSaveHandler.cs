using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class SceneSaveHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.SceneSave;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var scenePath = request.GetParam("scene", null);
            var saveAll = request.GetParam<bool>("all");

            if (saveAll)
            {
                return SaveAllDirtyScenes();
            }

            UnityEngine.SceneManagement.Scene scene;
            if (!string.IsNullOrEmpty(scenePath))
            {
                scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
                if (!scene.IsValid() || !scene.isLoaded)
                    return Fail(StatusCode.NotFound, $"Scene not loaded: {scenePath}");
            }
            else
            {
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            }

            var saved = EditorSceneManager.SaveScene(scene);
            if (!saved)
                return Fail(StatusCode.UnknownError, $"Failed to save scene: {scene.path}");

            return Ok($"Saved '{scene.path}'", new JObject
            {
                ["scenePath"] = scene.path,
                ["sceneDirty"] = scene.isDirty
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private CommandResponse SaveAllDirtyScenes()
        {
            var savedScenes = new List<string>();
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isDirty && scene.isLoaded)
                {
                    var saved = EditorSceneManager.SaveScene(scene);
                    if (saved)
                        savedScenes.Add(scene.path);
                }
            }

            return Ok($"Saved {savedScenes.Count} scene(s)", new JObject
            {
                ["savedScenes"] = JArray.FromObject(savedScenes),
                ["count"] = savedScenes.Count
            });
        }
#endif
    }
}
