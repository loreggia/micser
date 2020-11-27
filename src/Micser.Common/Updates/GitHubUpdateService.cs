using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Micser.Common.Updates.GitHub;
using Newtonsoft.Json;

namespace Micser.Common.Updates
{
    public class GitHubUpdateService : UpdateService
    {
        private const string AcceptHeader = "application/vnd.github.v3+json";
        private const string BaseUrl = "https://api.github.com";
        private const string GetLatestReleaseUrlPath = "/repos/loreggia/micser/releases/latest";
        private const string UserAgentHeader = "micser-update";

        public GitHubUpdateService(ILogger<GitHubUpdateService> logger)
            : base(logger)
        {
        }

        /// <inheritdoc />
        public override async Task<string> DownloadInstallerAsync(UpdateManifest manifest)
        {
            try
            {
                var path = Path.Combine(Globals.AppDataFolder, Globals.Updates.TempFolder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var fileName = Path.Combine(path, $"Micser-{manifest.Version}.msi");

                using var webClient = new WebClient();
                webClient.Headers.Add("User-Agent", UserAgentHeader);
                await webClient.DownloadFileTaskAsync(manifest.FileName, fileName).ConfigureAwait(false);

                return fileName;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to download installer. {manifest}");
                return null;
            }
        }

        public override async Task<UpdateManifest> GetUpdateManifestAsync()
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl, UriKind.Absolute),
            };
            client.DefaultRequestHeaders.Add("Accept", AcceptHeader);
            client.DefaultRequestHeaders.Add("User-Agent", UserAgentHeader);

            var response = await client.GetAsync(GetLatestReleaseUrlPath).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var streamReader = new StreamReader(stream);
            using var reader = new JsonTextReader(streamReader);
            var serializer = JsonSerializer.CreateDefault();

            var release = serializer.Deserialize<Release>(reader);

            //a.ContentType=="application/octet-stream"
            var asset = release?.Assets?.FirstOrDefault(a => a.Name?.EndsWith(".msi", StringComparison.InvariantCultureIgnoreCase) == true);
            if (asset?.DownloadUrl == null)
            {
                return null;
            }

            return new UpdateManifest
            {
                Date = release.PublishedAt,
                Description = release.Body,
                FileName = asset.DownloadUrl,
                Version = release.Name
            };
        }
    }
}