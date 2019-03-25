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

                _inClient = null;
                _outClient = null;

                _inClient = await _listener.AcceptTcpClientAsync();
                _inClient.Client.SetKeepAlive();
                var inStream = _inClient.GetStream();
                _inReader = new StreamReader(inStream);
                _inWriter = new StreamWriter(inStream) { AutoFlush = true };

                _outClient = await _listener.AcceptTcpClientAsync();
                _outClient.Client.SetKeepAlive();
                var outStream = _outClient.GetStream();
                _outReader = new StreamReader(outStream);
                _outWriter = new StreamWriter(outStream) { AutoFlush = true };

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
            _connectTask = ConnectAsync();
        }

        public void Stop()
        {
            IsRunning = false;
            _inClient?.Close();
            _outClient?.Close();
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