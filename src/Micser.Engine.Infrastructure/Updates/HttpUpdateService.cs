using NLog;
using System.Threading.Tasks;

namespace Micser.Engine.Infrastructure.Updates
{
    public class HttpUpdateService : UpdateService
    {
        public HttpUpdateService(ILogger logger)
            : base(logger)
        {
        }

        public override Task<string> DownloadInstallerAsync(UpdateManifest manifest)
        {
            throw new System.NotImplementedException();
        }

        public override Task<UpdateManifest> GetUpdateManifestAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}