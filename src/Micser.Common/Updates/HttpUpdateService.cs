﻿using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog;

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
        public HttpUpdateService(IOptions<HttpUpdateOptions> options, ILogger logger)
            : base(logger)
        {
            Options = options.Value ?? throw new MissingConfigurationException(Globals.AppSettingSections.Update.HttpUpdateSettings);
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

                using (var webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(manifest.FileName, fileName).ConfigureAwait(false);
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
                    var json = await webClient.DownloadStringTaskAsync(Options.ManifestUrl).ConfigureAwait(false);
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