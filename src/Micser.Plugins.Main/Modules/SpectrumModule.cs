using Micser.Common.Api;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Audio;
using System.Timers;

namespace Micser.Plugins.Main.Modules
{
    public class SpectrumModule : AudioModule
    {
        private readonly IApiClient _apiClient;
        private readonly SpectrumSampleProcessor _sampleProcessor;
        private readonly Timer _timer;
        private bool _isDisposed;

        public SpectrumModule(IApiClient apiClient)
        {
            _apiClient = apiClient;

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
                await _apiClient.SendMessageAsync(new ApiRequest("spectrum", null, data)).ConfigureAwait(false);
            }

            if (!_isDisposed)
            {
                _timer.Start();
            }
        }
    }
}