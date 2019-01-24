using Micser.Common;
using Micser.Engine.Infrastructure;
using Nancy.Hosting.Self;
using System;
using Unity;

namespace Micser.Engine.Api
{
    public class Server : IServer
    {
        private readonly IUnityContainer _container;
        private NancyHost _host;

        public Server(IUnityContainer container)
        {
            _container = container;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            _host?.Dispose();

            _host = new NancyHost(new Uri($"http://localhost:{Globals.ApiPort}"), new Bootstrapper(_container),
                                  new HostConfiguration { RewriteLocalhost = false });
            _host.Start();
        }

        public void Stop()
        {
            _host?.Dispose();
        }
    }
}