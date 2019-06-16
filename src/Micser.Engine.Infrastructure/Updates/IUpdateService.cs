using System.Threading.Tasks;

namespace Micser.Engine.Infrastructure.Updates
{
    public interface IUpdateService
    {
        Task<string> DownloadInstaller(UpdateManifest manifest);

        Task<UpdateManifest> GetUpdateManifest();
    }
}