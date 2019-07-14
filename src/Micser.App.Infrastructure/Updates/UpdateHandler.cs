using Micser.App.Infrastructure.Interaction;
using Micser.Common.Updates;
using Prism.Events;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Updates
{
    public class UpdateHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IUpdateService _updateService;

        public UpdateHandler(IUpdateService updateService, IEventAggregator eventAggregator)
        {
            _updateService = updateService;
            _eventAggregator = eventAggregator;
        }

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
                        Title = "Information",
                        Message = "There is currently no update available.",
                        Type = MessageBoxType.Information,
                        IsModal = true,
                    });
            }
        }

        public async Task InstallUpdateAsync(UpdateManifest manifest)
        {
            var fileName = await _updateService.DownloadInstallerAsync(manifest);
            string error = null;

            if (fileName == null)
            {
                error = "Could not download the installer. Please try again.";
            }
            else
            {
                var executeResult = _updateService.ExecuteInstaller(fileName);

                if (!executeResult)
                {
                    error = "Could not execute the installer. Please try again.";
                }
            }

            if (error != null)
            {
                _eventAggregator.GetEvent<MessageBoxEvent>().Publish(new MessageBoxEventArgs
                {
                    Title = "Error",
                    Message = error,
                    Type = MessageBoxType.Error,
                    IsModal = true
                });
            }
        }

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
                    Title = "Update available",
                    Type = MessageBoxType.Question
                });
        }
    }
}