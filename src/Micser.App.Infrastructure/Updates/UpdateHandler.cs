using Micser.App.Infrastructure.Interaction;
using Micser.App.Infrastructure.Properties;
using Micser.Common.Updates;
using Prism.Events;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Updates
{
    /// <summary>
    /// Handles interaction logic for application updates.
    /// </summary>
    public class UpdateHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IUpdateService _updateService;

        /// <inheritdoc />
        public UpdateHandler(IUpdateService updateService, IEventAggregator eventAggregator)
        {
            _updateService = updateService;
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// Checks for updates and shows a confirmation dialog if an update was found or an information dialog when no update is available.
        /// </summary>
        public async Task CheckForUpdateAsync()
        {
            var manifest = await _updateService.GetUpdateManifestAsync();

            if (_updateService.IsUpdateAvailable(manifest))
            {
                ShowUpdateAvailable(manifest);
            }
            else
            {
                _eventAggregator
                    .GetEvent<MessageBoxEvent>()
                    .Publish(new MessageBoxEventArgs
                    {
                        Title = Resources.Information,
                        Message = Resources.NoUpdateAvailable,
                        Type = MessageBoxType.Information,
                        IsModal = true,
                    });
            }
        }

        /// <summary>
        /// Installs an update. Shows an error message if the installation failed.
        /// </summary>
        /// <param name="manifest">The update manifest for of the update to install.</param>
        public async Task InstallUpdateAsync(UpdateManifest manifest)
        {
            var fileName = await _updateService.DownloadInstallerAsync(manifest);
            string error = null;

            if (fileName == null)
            {
                error = Resources.ErrorInstallerDownload;
            }
            else
            {
                var executeResult = _updateService.ExecuteInstaller(fileName);

                if (!executeResult)
                {
                    error = Resources.ErrorInstallerExecution;
                }
            }

            if (error != null)
            {
                _eventAggregator.GetEvent<MessageBoxEvent>().Publish(new MessageBoxEventArgs
                {
                    Title = Resources.Error,
                    Message = error,
                    Type = MessageBoxType.Error,
                    IsModal = true
                });
            }
        }

        /// <summary>
        /// Shows a confirmation to check if the user wants to install an available application update.
        /// </summary>
        /// <param name="manifest">The update manifest of the available update.</param>
        public void ShowUpdateAvailable(UpdateManifest manifest)
        {
            _eventAggregator
                .GetEvent<MessageBoxEvent>()
                .Publish(new MessageBoxEventArgs
                {
                    Callback = async result =>
                    {
                        if (result)
                        {
                            await InstallUpdateAsync(manifest);
                        }
                    },
                    IsModal = true,
                    Message = manifest.Description,
                    Title = Resources.UpdateAvailable,
                    Type = MessageBoxType.Question
                });
        }
    }
}