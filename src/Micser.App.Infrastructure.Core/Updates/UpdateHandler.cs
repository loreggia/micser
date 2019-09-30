using Micser.App.Infrastructure.Interaction;
using Micser.App.Infrastructure.Resources;
using Micser.Common.Updates;
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
                        Title = Strings.Information,
                        Message = Strings.NoUpdateAvailable,
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
                error = Strings.ErrorInstallerDownload;
            }
            else
            {
                var executeResult = _updateService.ExecuteInstaller(fileName);

                if (!executeResult)
                {
                    error = Strings.ErrorInstallerExecution;
                }
            }

            if (error != null)
            {
                _eventAggregator.GetEvent<MessageBoxEvent>().Publish(new MessageBoxEventArgs
                {
                    Title = Strings.Error,
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
            var message = string.Format(Strings.UpdateAvailableMessageFormat, manifest.Description);
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
                    Message = message,
                    Title = Strings.UpdateAvailable,
                    Type = MessageBoxType.Question
                });
        }
    }
}