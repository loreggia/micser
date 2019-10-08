using Micser.Common.Updates.GitHub;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Micser.Common.Updates
{
    public class GitHubUpdateService : UpdateService
    {
        private const string AcceptHeader = "application/vnd.github.v3+json";
        private const string BaseUrl = "https://api.github.com";
        private const string GetLatestReleaseUrlPath = "/repos/loreggia/micser/releases/latest";
        private const string UserAgentHeader = "micser-update";

        public GitHubUpdateService(ILogger logger)
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
                Logger.Error(ex);
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

            await using var stream = await client.GetStreamAsync(GetLatestReleaseUrlPath).ConfigureAwait(false);
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