using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptRenameSymbolHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptRenameSymbol;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var oldName = request.GetParam("oldName", null);
            var newName = request.GetParam("newName", null);
            if (string.IsNullOrEmpty(oldName))
                return InvalidParameters("Parameter 'oldName' is required.");
            if (string.IsNullOrEmpty(newName))
                return InvalidParameters("Parameter 'newName' is required.");

            if (oldName == newName)
                return InvalidParameters("'oldName' and 'newName' must be different.");

            // Validate identifier characters
            if (!IsValidIdentifier(newName))
                return InvalidParameters($"'{newName}' is not a valid C# identifier.");

            var folder = request.GetParam("folder", "Assets");
            var dryRun = request.GetParam<bool>("dryRun");

            // Find all MonoScript assets
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MonoScript",
                new[] { folder });

            var changedFiles = new JArray();
            int totalReplacements = 0;
            string renamedFile = null;

            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fullPath = System.IO.Path.Combine(
                    UnityEngine.Application.dataPath, "..", path);

                if (!System.IO.File.Exists(fullPath))
                    continue;

                try
                {
                    var content = System.IO.File.ReadAllText(fullPath);
                    var replaced = ReplaceWordBoundary(content, oldName, newName, out int count);

                    if (count == 0) continue;

                    totalReplacements += count;

                    if (!dryRun)
                        System.IO.File.WriteAllText(fullPath, replaced);

                    changedFiles.Add(new JObject
                    {
                        ["file"] = path,
                        ["replacements"] = count
                    });
                }
                catch (Exception ex)
                {
                    changedFiles.Add(new JObject
                    {
                        ["file"] = path,
                        ["error"] = ex.Message
                    });
                }
            }

            // Rename file if filename matches oldName
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!string.Equals(fileName, oldName, StringComparison.Ordinal))
                    continue;

                var newPath = path.Replace(oldName + ".cs", newName + ".cs");
                if (!dryRun)
                {
                    var moveResult = UnityEditor.AssetDatabase.MoveAsset(path, newPath);
                    if (!string.IsNullOrEmpty(moveResult))
                    {
                        return Fail(StatusCode.UnknownError,
                            $"Text replacement succeeded but file rename failed: {moveResult}",
                            new JObject
                            {
                                ["changedFiles"] = changedFiles,
                                ["totalReplacements"] = totalReplacements,
                                ["renameError"] = moveResult
                            });
                    }
                }
                renamedFile = $"{path} → {newPath}";
                break;
            }

            if (!dryRun)
                UnityEditor.AssetDatabase.Refresh();

            var message = dryRun
                ? $"[DRY RUN] Would replace {totalReplacements} occurrence(s) in {changedFiles.Count} file(s)"
                : $"Replaced {totalReplacements} occurrence(s) in {changedFiles.Count} file(s)";

            return Ok(message, new JObject
            {
                ["oldName"] = oldName,
                ["newName"] = newName,
                ["dryRun"] = dryRun,
                ["changedFiles"] = changedFiles,
                ["changedFileCount"] = changedFiles.Count,
                ["totalReplacements"] = totalReplacements,
                ["renamedFile"] = renamedFile,
                ["note"] = "Text-based replacement with word boundary matching. Verify compilation after rename."
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static string ReplaceWordBoundary(string content, string oldName, string newName, out int count)
        {
            count = 0;
            var result = new System.Text.StringBuilder(content.Length);
            int i = 0;

            while (i < content.Length)
            {
                int idx = content.IndexOf(oldName, i, StringComparison.Ordinal);
                if (idx < 0)
                {
                    result.Append(content, i, content.Length - i);
                    break;
                }

                // Check word boundaries
                bool leftOk = idx == 0 || !IsIdentChar(content[idx - 1]);
                int afterIdx = idx + oldName.Length;
                bool rightOk = afterIdx >= content.Length || !IsIdentChar(content[afterIdx]);

                if (leftOk && rightOk)
                {
                    result.Append(content, i, idx - i);
                    result.Append(newName);
                    i = afterIdx;
                    count++;
                }
                else
                {
                    result.Append(content, i, idx - i + 1);
                    i = idx + 1;
                }
            }

            return result.ToString();
        }

        private static bool IsIdentChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!char.IsLetter(name[0]) && name[0] != '_') return false;
            for (int i = 1; i < name.Length; i++)
            {
                if (!IsIdentChar(name[i])) return false;
            }
            return true;
        }
#endif
    }
}
