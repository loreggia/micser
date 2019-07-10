using Micser.Common.Updates;
using System.Threading.Tasks;

namespace Micser.Engine.Infrastructure.Updates
{
    /// <summary>
    /// Defines an interface for a service that can check for, download and execute application updates.
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// Downloads the installer for the release specified in the update manifest.
        /// </summary>
        /// <param name="manifest">The update manifest.</param>
        /// <returns>The full path to the downloaded installation package.</returns>
        Task<string> DownloadInstallerAsync(UpdateManifest manifest);

        /// <summary>
        /// Executes the installer located at the specified path.
        /// </summary>
        /// <param name="path">The file path where the installation package is located.</param>
        bool ExecuteInstaller(string path);

        /// <summary>
        /// Gets an update manifest containing information about the most recent release.
        /// </summary>
        Task<UpdateManifest> GetUpdateManifestAsync();
    }
}