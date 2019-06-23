using NLog;
using System.IO;
using System.Threading.Tasks;

namespace Micser.Engine.Infrastructure.Updates
{
    public class LocalUpdateService : UpdateService
    {
        /// <inheritdoc />
        public LocalUpdateService(ILogger logger)
            : base(logger)
        {
        }

        /// <inheritdoc />
        public override async Task<string> DownloadInstallerAsync(UpdateManifest manifest)
        {
            return Path.GetFullPath("Micser.msi");
        }

        /// <inheritdoc />
        public override async Task<UpdateManifest> GetUpdateManifestAsync()
        {
            return new UpdateManifest
            {
                FileName = "Micser.msi"
            };
        }
    }
}