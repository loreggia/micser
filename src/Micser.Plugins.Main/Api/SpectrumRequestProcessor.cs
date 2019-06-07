using Micser.Common.Api;
using Prism.Events;

namespace Micser.Plugins.Main.Api
{
    [RequestProcessorName("spectrum")]
    public class SpectrumRequestProcessor : RequestProcessor
    {
        private readonly IEventAggregator _eventAggregator;

        public SpectrumRequestProcessor(IEventAggregator eventAggregator)
        {
            this[""] = data => Process(data);
            _eventAggregator = eventAggregator;
        }

        public object Process(SpectrumData data)
        {
            if (data == null)
            {
                return false;
            }

            _eventAggregator.GetEvent<SpectrumDataEvent>().Publish(data);
            return true;
        }
    }
}