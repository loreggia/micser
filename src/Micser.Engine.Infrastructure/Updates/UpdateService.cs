using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Micser.Engine.Infrastructure.Updates
{
    public abstract class UpdateService : IUpdateService
    {
        protected readonly ILogger Logger;

        protected UpdateService(ILogger logger)
        {
            Logger = logger;
        }

        public abstract Task<string> DownloadInstallerAsync(UpdateManifest manifest);

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

        public abstract Task<UpdateManifest> GetUpdateManifestAsync();
    }
}