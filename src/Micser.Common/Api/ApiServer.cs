using Micser.Common.Extensions;
using NLog;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 4014

namespace Micser.Common.Api
{
    public class ApiServer : ApiEndPoint, IApiServer
    {
        private readonly TcpListener _listener;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _startSemaphore;

        public ApiServer(IRequestProcessorFactory requestProcessorFactory, ILogger logger)
            : base(requestProcessorFactory)
        {
            _logger = logger;
            var endPoint = new IPEndPoint(IPAddress.Loopback, Globals.ApiPort);
            _listener = new TcpListener(endPoint);
            _startSemaphore = new SemaphoreSlim(1, 1);
        }

        public bool IsRunning { get; private set; }

        public override async Task ConnectAsync()
        {
            try
            {
                await _startSemaphore.WaitAsync();

                InClient = null;
                OutClient = null;

                InClient = await _listener.AcceptTcpClientAsync();
                InClient.Client.SetKeepAlive();
                var inStream = InClient.GetStream();
                InReader = new StreamReader(inStream);
                InWriter = new StreamWriter(inStream) { AutoFlush = true };

                OutClient = await _listener.AcceptTcpClientAsync();
                OutClient.Client.SetKeepAlive();
                var outStream = OutClient.GetStream();
                OutReader = new StreamReader(outStream);
                OutWriter = new StreamWriter(outStream) { AutoFlush = true };

                IsRunning = true;

                Task.Run(() => ReaderThread());
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            finally
            {
                _startSemaphore.Release();
            }
        }

        public void Start()
        {
            _listener.Start();
            ConnectTask = ConnectAsync();
        }

        public void Stop()
        {
            IsRunning = false;
            InClient?.Close();
            OutClient?.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listener.Stop();
                Stop();
            }

            base.Dispose(disposing);
        }
    }
}