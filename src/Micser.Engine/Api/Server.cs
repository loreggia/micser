using Micser.Common;
using Micser.Common.Api;
using Micser.Engine.Infrastructure;
using NLog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity;

namespace Micser.Engine.Api
{
    public class Server : IServer
    {
        private readonly IUnityContainer _container;
        private readonly ILogger _logger;
        private Socket _listener;

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

            var hostEntry = Dns.GetHostEntry(IPAddress.Loopback);
            var address = hostEntry.AddressList[0];
            var endPoint = new IPEndPoint(address, Globals.ApiPort);
            _listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(endPoint);
            _listener.Listen(5);

            _listener.BeginAccept(AcceptCallback, null);

            _logger.Info("API server started");
        }

        public void Stop()
        {
            _logger.Info("Stopping API server");

            _logger.Info("API server stopped");
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            var socket = _listener.EndAccept(ar);
            var buffer = new MessageBuffer(socket, OnMessageReceived);
            buffer.BeginReceive();
        }

        private void OnMessageReceived(StringBuilder content)
        {
        }
    }
}