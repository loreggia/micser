using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Updates;
using Prism.Events;

namespace Micser.App.Infrastructure.Api
{
    [RequestProcessorName(Globals.ApiResources.Updates)]
    public class UpdatesRequestProcessor : RequestProcessor
    {
        private readonly IEventAggregator _eventAggregator;

        public UpdatesRequestProcessor(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            AddAction("UpdateAvailable", p => UpdateAvailable(p));
        }

        private object UpdateAvailable(UpdateManifest updateManifest)
        {
        }
    }
}