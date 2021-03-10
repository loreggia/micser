using System;
using Newtonsoft.Json;

#pragma warning disable 1591

namespace Micser.Common.Updates.GitHub
{
    public class Release
    {
        [JsonProperty("assets")]
        public Asset[]? Assets { get; set; }

        [JsonProperty("body")]
        public string? Body { get; set; }

        [JsonProperty("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }
    }
}