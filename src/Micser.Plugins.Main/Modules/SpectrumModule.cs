using System.Timers;
using Microsoft.Extensions.Logging;
using Micser.Common.Api;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Api;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class SpectrumModule : AudioModule
    {
        private readonly SpectrumSampleProcessor _sampleProcessor;
        private readonly IRpcStreamService<SpectrumData> _streamService;
        private readonly Timer _timer;
        private bool _isDisposed;

        public SpectrumModule(IRpcStreamService<SpectrumData> streamService, ILogger<SpectrumModule> logger)
            : base(logger)
        {
            _streamService = streamService;
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

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_isDisposed)
            {
                return;
            }

            var data = _sampleProcessor.GetFftData();
            if (data != null)
            {
                await _streamService.SendMessageAsync(data);
            }

            if (!_isDisposed)
            {
                _timer.Start();
            }
        }
    }
}