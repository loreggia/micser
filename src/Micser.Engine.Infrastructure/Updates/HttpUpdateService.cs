using Micser.Common.Updates;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Micser.Engine.Infrastructure.Updates
{
    /// <summary>
    /// An update service that downloads manifest/installer data from a HTTP resource.
    /// </summary>
    public class HttpUpdateService : UpdateService
    {
        /// <summary>
        /// Gets the update settings (manifest URL, ...).
        /// </summary>
        protected readonly HttpUpdateSettings Settings;

        /// <inheritdoc />
        public HttpUpdateService(HttpUpdateSettings settings, ILogger logger)
            : base(logger)
        {
            Settings = settings;
        }

        /// <inheritdoc />
        public override async Task<string> DownloadInstallerAsync(UpdateManifest manifest)
        {
            try
            {
                var tempPath = Path.GetTempPath();
                var fileName = Path.Combine(tempPath, Path.ChangeExtension(Path.GetRandomFileName(), ".msi"));

                using (var webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(manifest.FileName, fileName);
                }

                return fileName;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        /// <inheritdoc />
        public override async Task<UpdateManifest> GetUpdateManifestAsync()
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    var json = await webClient.DownloadStringTaskAsync(Settings.ManifestUrl);
                    return JsonConvert.DeserializeObject<UpdateManifest>(json);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }
    }
}