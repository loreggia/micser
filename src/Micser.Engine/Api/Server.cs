using Micser.Engine.Audio;
using Micser.Shared;
using Nancy.Hosting.Self;
using System;

namespace Micser.Engine.Api
{
    public class Server : IDisposable
    {
        private readonly IAudioEngine _audioEngine;
        private NancyHost _host;

        public Server(IAudioEngine audioEngine)
        {
            _audioEngine = audioEngine;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            _host?.Dispose();

            _host = new NancyHost(new Uri($"http://localhost:{Globals.ApiPort}"), new Bootstrapper(_audioEngine), new HostConfiguration { RewriteLocalhost = false });
            _host.Start();
        }

        public void Stop()
        {
            _host?.Dispose();
        }
    }
}