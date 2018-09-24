using Micser.Shared;
using Nancy.Hosting.Self;
using System;

namespace Micser.Engine.Api
{
    public class Server : IDisposable
    {
        private NancyHost _host;

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            _host?.Dispose();

            _host = new NancyHost(new Uri($"http://localhost:{Globals.ApiPort}"), new Bootstrapper(), new HostConfiguration { RewriteLocalhost = false });
            _host.Start();
        }

        public void Stop()
        {
            _host?.Dispose();
        }
    }
}