using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unityctl.Plugin.Editor.Shared
{
    /// <summary>
    /// Streaming event wrapper for IPC watch mode.
    /// Plugin-side copy using Newtonsoft.Json (wire format identical to Shared version).
    /// </summary>
    public sealed class EventEnvelope
    {
        [JsonProperty("channel")]
        public string channel = string.Empty;

        [JsonProperty("eventType")]
        public string eventType = string.Empty;

        [JsonProperty("timestamp")]
        public long timestamp;

        [JsonProperty("sessionId")]
        public string sessionId;

        [JsonProperty("payload")]
        public JObject payload;

        public static EventEnvelope Create(string channel, string eventType, JObject payload = null)
        {
            return new EventEnvelope
            {
                channel = channel,
                eventType = eventType,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                payload = payload
            };
        }
    }
}
