using System.IO;
using System.Threading.Tasks;

namespace Micser.Engine.Infrastructure.Updates
{
    public class LocalUpdateService : IUpdateService
    {
        public async Task<string> DownloadInstaller(UpdateManifest manifest)
        {
            return Path.GetFullPath("Micser.msi");
        }

        public async Task<UpdateManifest> GetUpdateManifest()
        {
            return new UpdateManifest
            {
                FileName = "Micser.msi"
            };
        }
    }
}