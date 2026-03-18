using Newtonsoft.Json;

namespace Unityctl.Plugin.Editor.Shared
{
    public sealed class PreflightCheck
    {
        [JsonProperty("category")]
        public string category;

        [JsonProperty("check")]
        public string check;

        [JsonProperty("passed")]
        public bool passed;

        [JsonProperty("message")]
        public string message;

        [JsonProperty("details")]
        public string details;
    }
}
