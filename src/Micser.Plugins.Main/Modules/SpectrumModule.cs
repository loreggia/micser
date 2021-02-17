using System.Timers;
using Microsoft.Extensions.Logging;
using Micser.Common.Audio;
using Micser.Common.Events;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class SpectrumModule : AudioModule
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly SpectrumSampleProcessor _sampleProcessor;
        private readonly Timer _timer;
        private bool _isDisposed;

        public SpectrumModule(IEventAggregator eventAggregator, ILogger<SpectrumModule> logger)
            : base(logger)
        {
            _eventAggregator = eventAggregator;
            _sampleProcessor = new SpectrumSampleProcessor();
            AddSampleProcessor(_sampleProcessor);

            _timer = new Timer(50) { AutoReset = false };
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _timer.Stop();
            _timer.Dispose();

            _isDisposed = true;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_isDisposed)
            {
                return;
            }

            var data = _sampleProcessor.GetFftData();
            if (data != null)
            {
                _eventAggregator.Publish(data);
            }

            if (!_isDisposed)
            {
                _timer.Start();
            }
        }
    }
}