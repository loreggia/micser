using Newtonsoft.Json;

namespace Micser.Common.Updates.GitHub
{
    public class Asset
    {
        [JsonProperty("content_type")]
        public string? ContentType { get; set; }

        [JsonProperty("browser_download_url")]
        public string? DownloadUrl { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("state")]
        public string? State { get; set; }
    }
}