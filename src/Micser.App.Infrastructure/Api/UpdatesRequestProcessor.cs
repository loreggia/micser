using Micser.App.Infrastructure.Interaction;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Updates;
using Prism.Events;
using System;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// Handles notifications from the engine when an application update is available.
    /// </summary>
    [RequestProcessorName(Globals.ApiResources.Updates)]
    public class UpdatesRequestProcessor : RequestProcessor
    {
        private readonly IEventAggregator _eventAggregator;

        /// <inheritdoc />
        public UpdatesRequestProcessor(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            AddAction("UpdateAvailable", p => UpdateAvailable(p));
        }

        private object UpdateAvailable(UpdateManifest updateManifest)
        {
            _eventAggregator.GetEvent<MessageBoxEvent>().Publish(new MessageBoxEventArgs
            {
                Type = MessageBoxType.Question,
                Title = "Update available",
                Message = updateManifest.Description,
                Callback = UpdateConfirmationCallback,
                IsModal = true
            });

            return true;
        }

        private void UpdateConfirmationCallback(bool install)
        {
            Console.WriteLine(install);
        }
    }
}