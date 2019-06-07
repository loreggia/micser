using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.Widgets;
using Micser.Plugins.Main.Api;
using Micser.Plugins.Main.Modules;
using Prism.Events;
using System;
using System.Diagnostics;

namespace Micser.Plugins.Main.Widgets
{
    public class SpectrumViewModel : WidgetViewModel
    {
        public const string InputConnectorName = "Input1";
        public const string OutputConnectorName = "Output1";
        private readonly IEventAggregator _eventAggregator;
        private SubscriptionToken _subToken;

        public SpectrumViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            AddInput(InputConnectorName);
            AddOutput(OutputConnectorName);
        }

        public override Type ModuleType => typeof(SpectrumModule);

        public override void Initialize()
        {
            base.Initialize();

            _subToken = _eventAggregator.GetEvent<SpectrumDataEvent>().Subscribe(OnSpectrumDataEvent);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_subToken != null)
                {
                    _eventAggregator.GetEvent<ApiEvent>().Unsubscribe(_subToken);
                    _subToken = null;
                }
            }
        }

        private void OnSpectrumDataEvent(SpectrumData data)
        {
            Debug.WriteLine(data);
        }
    }
}