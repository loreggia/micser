using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Micser.Common.Updates
{
    /// <summary>
    /// Base implementation of <see cref="IUpdateService"/> that provides functionality for executing an installer.
    /// </summary>
    public abstract class UpdateService : IUpdateService
    {
        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger Logger;

        protected UpdateService(ILogger logger)
        {
            Logger = logger;
        }

        /// <inheritdoc />
        public abstract Task<string?> DownloadInstallerAsync(UpdateManifest manifest);

        /// <summary>
        /// Executes a MSI installer.
        /// </summary>
        /// <param name="path">The path of the MSI installer.</param>
        public virtual bool ExecuteInstaller(string path)
        {
            if (!File.Exists(path))
            {
                Logger.LogError($"The file '{path}' does not exist.");
                return false;
            }

            try
            {
                var extension = Path.GetExtension(path).Trim('.').ToLower();

                if (extension == "msi")
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo("msiexec", $"/i \"{Path.GetFullPath(path)}\"")
                    };
                    return process.Start();
                }

                Logger.LogError($"Only MSI files are supported. ({path})");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to execute installer at '{path}'.");
                return false;
            }
        }

        /// <inheritdoc />
        public abstract Task<UpdateManifest?> GetUpdateManifestAsync();

        /// <inheritdoc />
        public virtual bool IsUpdateAvailable(UpdateManifest manifest)
        {
            if (manifest.Version == null)
            {
                Logger.LogError("The update manifest's version is not set.");
                return false;
            }

            var updateVersion = new Version(manifest.Version);
            var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version;

            return updateVersion > currentVersion;
        }
    }
}