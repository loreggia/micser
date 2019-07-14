using Micser.App.Infrastructure.Updates;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Updates;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// Handles notifications from the engine when an application update is available.
    /// </summary>
    [RequestProcessorName(Globals.ApiResources.Updates)]
    public class UpdatesRequestProcessor : RequestProcessor
    {
        private readonly UpdateHandler _updateHandler;

        /// <inheritdoc />
        public UpdatesRequestProcessor(UpdateHandler updateHandler)
        {
            _updateHandler = updateHandler;
            AddAction("UpdateAvailable", p => UpdateAvailable(p));
        }

        private object UpdateAvailable(UpdateManifest updateManifest)
        {
            _updateHandler.ShowUpdateAvailable(updateManifest);
            return true;
        }
    }
}