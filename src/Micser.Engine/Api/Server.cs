using Micser.Infrastructure;
using Nancy.Hosting.Self;
using System;
using Unity;

namespace Micser.Engine.Api
{
    public class Server : IDisposable
    {
        private NancyHost _host;

        public Server()
        {
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start(IUnityContainer container)
        {
            _host?.Dispose();

            _host = new NancyHost(new Uri($"http://localhost:{Globals.ApiPort}"), new Bootstrapper(container), new HostConfiguration { RewriteLocalhost = false });
            _host.Start();
        }

        public void Stop()
        {
            _host?.Dispose();
        }
    }
}