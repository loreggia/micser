using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Micser.Common.Updates
{
    /// <summary>
    /// An update service that downloads manifest/installer data from a HTTP resource.
    /// </summary>
    public class HttpUpdateService : UpdateService
    {
        /// <summary>
        /// Gets the update settings (manifest URL, ...).
        /// </summary>
        protected readonly HttpUpdateOptions Options;

        /// <inheritdoc />
        public HttpUpdateService(IOptions<HttpUpdateOptions> options, ILogger<HttpUpdateService> logger)
            : base(logger)
        {
            Options = options.Value ?? throw new MissingConfigurationException(Globals.AppSettingSections.Update.HttpUpdateSettings);
        }

        /// <inheritdoc />
        public override async Task<string?> DownloadInstallerAsync(UpdateManifest manifest)
        {
            if (manifest.FileName == null)
            {
                Logger.LogError($"The update manifest's file name is null.");
                return null;
            }

            try
            {
                var path = Path.Combine(Globals.AppDataFolder, Globals.Updates.TempFolder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var fileName = Path.Combine(path, $"Micser-{manifest.Version}.msi");

                using (var webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(manifest.FileName, fileName).ConfigureAwait(false);
                }

                return fileName;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to download installer. {manifest}");
                return null;
            }
        }

        /// <inheritdoc />
        public override async Task<UpdateManifest?> GetUpdateManifestAsync()
        {
            if (Options.ManifestUrl == null)
            {
                Logger.LogError("The manifest URL is not set in the update options.");
                return null;
            }

            try
            {
                using var webClient = new WebClient();

                var json = await webClient.DownloadStringTaskAsync(Options.ManifestUrl).ConfigureAwait(false);
                return JsonConvert.DeserializeObject<UpdateManifest>(json);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get update manifest.");
                return null;
            }
        }
    }
}