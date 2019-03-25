using Micser.Common.Extensions;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

#pragma warning disable 4014

namespace Micser.Common.Api
{
    public class ApiServer : ApiEndPoint, IApiServer
    {
        private readonly TcpListener _listener;
        private bool _isStarting;

        public ApiServer(IRequestProcessorFactory requestProcessorFactory)
            : base(requestProcessorFactory)
        {
            var endPoint = new IPEndPoint(IPAddress.Loopback, Globals.ApiPort);
            _listener = new TcpListener(endPoint);
        }

        public bool IsRunning { get; private set; }

        public override async Task ConnectAsync()
        {
            if (_isStarting)
            {
                return;
            }

            try
            {
                _isStarting = true;

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
                Console.WriteLine(ex);
            }
            finally
            {
                _isStarting = false;
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