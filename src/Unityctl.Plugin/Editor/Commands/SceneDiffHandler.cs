using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    /// <summary>
    /// Handles the scene-diff command: compares two scene snapshots within the Unity Editor process.
    /// Accepts two full snapshot JObjects as parameters and returns a SceneDiffResult.
    /// </summary>
    public class SceneDiffHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.SceneDiff;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var baseSnap = request.GetObjectParam("base");
            var headSnap = request.GetObjectParam("head");

            if (baseSnap == null)
                return InvalidParameters("Parameter 'base' is required (snapshot JObject)");
            if (headSnap == null)
                return InvalidParameters("Parameter 'head' is required (snapshot JObject)");

            // Parse epsilon — stored as JToken, retrieve as double
            double epsilon = 1e-6;
            var epsilonNode = request.parameters?["epsilon"];
            if (epsilonNode != null)
            {
                try { epsilon = epsilonNode.Value<double>(); }
                catch { /* use default */ }
            }

            var baseScenes = BuildSceneDict(baseSnap["scenes"] as JArray ?? new JArray());
            var headScenes = BuildSceneDict(headSnap["scenes"] as JArray ?? new JArray());

            var sceneDiffs = new JArray();
            var allScenePaths = new HashSet<string>(baseScenes.Keys);
            allScenePaths.UnionWith(headScenes.Keys);

            foreach (var path in allScenePaths)
            {
                baseScenes.TryGetValue(path, out var baseScene);
                headScenes.TryGetValue(path, out var headScene);

                if (baseScene == null || headScene == null) continue;

                var diff = DiffScene(path, baseScene, headScene, epsilon);
                if (HasAnyChanges(diff))
                    sceneDiffs.Add(diff);
            }

            var result = new JObject
            {
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["baseSnapshot"] = baseSnap["timestamp"]?.ToString() ?? string.Empty,
                ["headSnapshot"] = headSnap["timestamp"]?.ToString() ?? string.Empty,
                ["scenes"] = sceneDiffs
            };

            return Ok("Scene diff computed", result);
#else
            return NotInEditor();
#endif
        }

        protected override CommandResponse HandleException(Exception exception)
        {
            return Fail(StatusCode.UnknownError, $"Scene diff failed: {exception.Message}",
                errors: GetStackTrace(exception));
        }

#if UNITY_EDITOR
        private static Dictionary<string, JObject> BuildSceneDict(JArray scenes)
        {
            var dict = new Dictionary<string, JObject>(StringComparer.Ordinal);
            foreach (JObject scene in scenes)
            {
                var path = scene["path"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(path))
                    dict[path] = scene;
            }
            return dict;
        }

        private static JObject DiffScene(string scenePath, JObject baseScene, JObject headScene, double epsilon)
        {
            var baseObjects = BuildObjectDict(baseScene["gameObjects"] as JArray ?? new JArray());
            var headObjects = BuildObjectDict(headScene["gameObjects"] as JArray ?? new JArray());

            var addedObjects = new JArray();
            var removedObjects = new JArray();
            var modifiedObjects = new JArray();

            foreach (var id in headObjects.Keys)
            {
                if (!baseObjects.ContainsKey(id))
                {
                    var obj = headObjects[id];
                    addedObjects.Add(new JObject
                    {
                        ["globalObjectId"] = id,
                        ["name"] = obj["name"]?.ToString() ?? string.Empty,
                        ["scenePath"] = obj["scenePath"]?.ToString() ?? string.Empty
                    });
                }
            }

            foreach (var id in baseObjects.Keys)
            {
                if (!headObjects.ContainsKey(id))
                {
                    var obj = baseObjects[id];
                    removedObjects.Add(new JObject
                    {
                        ["globalObjectId"] = id,
                        ["name"] = obj["name"]?.ToString() ?? string.Empty,
                        ["scenePath"] = obj["scenePath"]?.ToString() ?? string.Empty
                    });
                }
            }

            foreach (var id in baseObjects.Keys)
            {
                if (!headObjects.ContainsKey(id)) continue;
                var objDiff = DiffGameObject(id, baseObjects[id], headObjects[id], epsilon);
                if (objDiff != null)
                    modifiedObjects.Add(objDiff);
            }

            return new JObject
            {
                ["scenePath"] = scenePath,
                ["addedObjects"] = addedObjects,
                ["removedObjects"] = removedObjects,
                ["modifiedObjects"] = modifiedObjects
            };
        }

        private static Dictionary<string, JObject> BuildObjectDict(JArray objects)
        {
            var dict = new Dictionary<string, JObject>(StringComparer.Ordinal);
            foreach (JObject obj in objects)
            {
                var id = obj["globalObjectId"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(id) && !dict.ContainsKey(id))
                    dict[id] = obj;
            }
            return dict;
        }

        private static JObject DiffGameObject(
            string objectId,
            JObject baseObj,
            JObject headObj,
            double epsilon)
        {
            var baseComps = BuildComponentDict(baseObj["components"] as JArray ?? new JArray());
            var headComps = BuildComponentDict(headObj["components"] as JArray ?? new JArray());

            var addedComps = new JArray();
            var removedComps = new JArray();
            var modifiedComps = new JArray();

            foreach (var typeName in headComps.Keys)
            {
                if (!baseComps.ContainsKey(typeName))
                    addedComps.Add(new JObject
                    {
                        ["typeName"] = typeName,
                        ["globalObjectId"] = headComps[typeName]["globalObjectId"]?.ToString() ?? string.Empty
                    });
            }

            foreach (var typeName in baseComps.Keys)
            {
                if (!headComps.ContainsKey(typeName))
                    removedComps.Add(new JObject
                    {
                        ["typeName"] = typeName,
                        ["globalObjectId"] = baseComps[typeName]["globalObjectId"]?.ToString() ?? string.Empty
                    });
            }

            foreach (var typeName in baseComps.Keys)
            {
                if (!headComps.ContainsKey(typeName)) continue;
                var compDiff = DiffComponent(typeName, baseComps[typeName], headComps[typeName], epsilon);
                if (compDiff != null)
                    modifiedComps.Add(compDiff);
            }

            if (addedComps.Count == 0 && removedComps.Count == 0 && modifiedComps.Count == 0)
                return null;

            return new JObject
            {
                ["globalObjectId"] = objectId,
                ["name"] = baseObj["name"]?.ToString() ?? string.Empty,
                ["addedComponents"] = addedComps,
                ["removedComponents"] = removedComps,
                ["modifiedComponents"] = modifiedComps
            };
        }

        private static Dictionary<string, JObject> BuildComponentDict(JArray components)
        {
            var dict = new Dictionary<string, JObject>(StringComparer.Ordinal);
            foreach (JObject comp in components)
            {
                var typeName = comp["typeName"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(typeName) && !dict.ContainsKey(typeName))
                    dict[typeName] = comp;
            }
            return dict;
        }

        private static JObject DiffComponent(
            string typeName,
            JObject baseComp,
            JObject headComp,
            double epsilon)
        {
            var baseProps = baseComp["properties"] as JObject ?? new JObject();
            var headProps = headComp["properties"] as JObject ?? new JObject();

            var changes = new JArray();

            // Properties in base — check for changes or removals
            foreach (var kvp in baseProps)
            {
                var path = kvp.Key;
                var baseVal = kvp.Value;
                var headVal = headProps[path];

                if (!AreJTokensEqual(baseVal, headVal, epsilon))
                {
                    changes.Add(new JObject
                    {
                        ["propertyPath"] = path,
                        ["oldValue"] = JTokenToString(baseVal),
                        ["newValue"] = JTokenToString(headVal),
                        ["valueType"] = GetValueType(baseVal)
                    });
                }
            }

            // Properties only in head (added)
            foreach (var kvp in headProps)
            {
                if (baseProps[kvp.Key] == null)
                {
                    changes.Add(new JObject
                    {
                        ["propertyPath"] = kvp.Key,
                        ["oldValue"] = string.Empty,
                        ["newValue"] = JTokenToString(kvp.Value),
                        ["valueType"] = GetValueType(kvp.Value)
                    });
                }
            }

            if (changes.Count == 0) return null;

            return new JObject
            {
                ["typeName"] = typeName,
                ["propertyChanges"] = changes
            };
        }

        private static bool AreJTokensEqual(JToken a, JToken b, double epsilon)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            // Float comparison with epsilon
            if (a.Type == JTokenType.Float || a.Type == JTokenType.Integer)
            {
                if (b.Type == JTokenType.Float || b.Type == JTokenType.Integer)
                {
                    double da = a.Value<double>();
                    double db = b.Value<double>();
                    return Math.Abs(da - db) <= epsilon;
                }
            }

            return JToken.DeepEquals(a, b);
        }

        private static string JTokenToString(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return string.Empty;
            if (token.Type == JTokenType.String) return token.Value<string>() ?? string.Empty;
            return token.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static string GetValueType(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return "null";
            return token.Type switch
            {
                JTokenType.Float => "float",
                JTokenType.Integer => "int",
                JTokenType.String => "string",
                JTokenType.Boolean => "bool",
                JTokenType.Object => "object",
                _ => "unknown"
            };
        }

        private static bool HasAnyChanges(JObject diff)
        {
            return (diff["addedObjects"] as JArray)?.Count > 0
                || (diff["removedObjects"] as JArray)?.Count > 0
                || (diff["modifiedObjects"] as JArray)?.Count > 0;
        }
#endif
    }
}
