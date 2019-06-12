using Micser.Common.Api;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Audio;
using System.Timers;

namespace Micser.Plugins.Main.Modules
{
    public class SpectrumModule : AudioModule
    {
        private readonly IApiEndPoint _apiEndPoint;
        private readonly SpectrumSampleProcessor _sampleProcessor;
        private readonly Timer _timer;

        public SpectrumModule(IApiEndPoint apiEndPoint)
        {
            _apiEndPoint = apiEndPoint;

            _sampleProcessor = new SpectrumSampleProcessor(this);
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
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var data = _sampleProcessor.Data;
            if (data != null)
            {
                await _apiEndPoint.SendMessageAsync(new JsonRequest("spectrum", null, data));
            }
            _timer.Start();
        }
    }
}