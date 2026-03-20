using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ConsoleGetEntriesHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ConsoleGetEntries;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var limitStr = request.GetParam("limit", "50");
            var filter = request.GetParam("filter", null); // "error", "warning", "log", or null for all
            int limit = 50;
            if (int.TryParse(limitStr, out var parsed)) limit = parsed;

            var logEntriesType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntriesType == null)
                return Fail(StatusCode.UnknownError, "Cannot access LogEntries API via reflection.");

            var startMethod = logEntriesType.GetMethod("StartGettingEntries",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var endMethod = logEntriesType.GetMethod("EndGettingEntries",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var getCountMethod = logEntriesType.GetMethod("GetCount",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (startMethod == null || endMethod == null || getCountMethod == null || getEntryMethod == null)
                return Fail(StatusCode.UnknownError, "LogEntries reflection methods not found.");

            var logEntryType = System.Type.GetType("UnityEditor.LogEntry, UnityEditor");
            if (logEntryType == null)
                return Fail(StatusCode.UnknownError, "LogEntry type not found.");

            startMethod.Invoke(null, null);
            try
            {
                int totalCount = (int)getCountMethod.Invoke(null, null);
                var deduped = new Dictionary<string, DedupedEntry>();
                var order = new List<string>();

                var entry = System.Activator.CreateInstance(logEntryType);
                var messageField = logEntryType.GetField("message") ?? logEntryType.GetField("condition");
                var modeField = logEntryType.GetField("mode");

                int processed = 0;
                for (int i = totalCount - 1; i >= 0 && processed < limit * 10; i--)
                {
                    getEntryMethod.Invoke(null, new object[] { i, entry });
                    var message = messageField?.GetValue(entry)?.ToString() ?? "";
                    int mode = modeField != null ? (int)modeField.GetValue(entry) : 0;

                    var entryType = ClassifyMode(mode);
                    if (!string.IsNullOrEmpty(filter) &&
                        !string.Equals(filter, entryType, System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Extract first line as key for dedup
                    var firstLine = GetFirstLine(message);
                    var key = $"{entryType}:{firstLine}";

                    if (deduped.TryGetValue(key, out var existing))
                    {
                        existing.Count++;
                        existing.LastIndex = i;
                    }
                    else
                    {
                        if (deduped.Count >= limit) break;
                        var d = new DedupedEntry
                        {
                            Message = firstLine,
                            Type = entryType,
                            Count = 1,
                            FirstIndex = i,
                            LastIndex = i
                        };
                        deduped[key] = d;
                        order.Add(key);
                    }
                    processed++;
                }

                var entries = new JArray();
                foreach (var k in order)
                {
                    var d = deduped[k];
                    entries.Add(new JObject
                    {
                        ["message"] = d.Message,
                        ["type"] = d.Type,
                        ["count"] = d.Count,
                        ["firstIndex"] = d.FirstIndex,
                        ["lastIndex"] = d.LastIndex
                    });
                }

                return Ok($"Console entries: {entries.Count} unique (deduped)", new JObject
                {
                    ["totalEntries"] = totalCount,
                    ["uniqueEntries"] = entries.Count,
                    ["entries"] = entries
                });
            }
            finally
            {
                endMethod.Invoke(null, null);
            }
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static string ClassifyMode(int mode)
        {
            // Unity LogEntry mode flags: bit 0 = error, bit 1 = warning, bit 9 = ScriptingError, etc.
            if ((mode & 0x201) != 0) return "error";
            if ((mode & 0x102) != 0) return "warning";
            return "log";
        }

        private static string GetFirstLine(string message)
        {
            if (string.IsNullOrEmpty(message)) return "";
            var idx = message.IndexOf('\n');
            return idx >= 0 ? message.Substring(0, idx) : message;
        }

        private sealed class DedupedEntry
        {
            public string Message;
            public string Type;
            public int Count;
            public int FirstIndex;
            public int LastIndex;
        }
#endif
    }
}
