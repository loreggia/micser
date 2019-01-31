using Micser.Common;
using Micser.Engine.Infrastructure;
using Nancy.Hosting.Self;
using NLog;
using System;
using Unity;

namespace Micser.Engine.Api
{
    public class Server : IServer
    {
        private readonly IUnityContainer _container;
        private readonly ILogger _logger;
        private NancyHost _host;

        public Server(IUnityContainer container, ILogger logger)
        {
            _container = container;
            _logger = logger;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            _logger.Info("Starting API server");

            _host?.Dispose();

            _host = new NancyHost(new Uri($"http://localhost:{Globals.ApiPort}"), new Bootstrapper(_container),
                                  new HostConfiguration { RewriteLocalhost = false });
            _host.Start();

            _logger.Info("API server started");
        }

        public void Stop()
        {
            _logger.Info("Stopping API server");

            _host?.Dispose();

            _logger.Info("API server stopped");
        }
    }
}