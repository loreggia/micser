using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Micser.Engine.Infrastructure.Updates
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

        /// <inheritdoc />
        protected UpdateService(ILogger logger)
        {
            Logger = logger;
        }

        /// <inheritdoc />
        public abstract Task<string> DownloadInstallerAsync(UpdateManifest manifest);

        /// <summary>
        /// Executes a MSI installer.
        /// </summary>
        /// <param name="path">The path of the MSI installer.</param>
        public virtual bool ExecuteInstaller(string path)
        {
            if (path == null)
            {
                return false;
            }

            if (!File.Exists(path))
            {
                Logger.Error($"The file '{path}' does not exist.");
                return false;
            }

            try
            {
                var extension = Path.GetExtension(path).Trim('.').ToLower();

                if (extension == "msi")
                {
                    var process = new Process();
                    process.StartInfo = new ProcessStartInfo("msiexec", $"/i \"{Path.GetExtension(path)}\"");
                    return process.Start();
                }

                Logger.Error($"Only MSI files are supported. ({path})");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        /// <inheritdoc />
        public abstract Task<UpdateManifest> GetUpdateManifestAsync();
    }
}