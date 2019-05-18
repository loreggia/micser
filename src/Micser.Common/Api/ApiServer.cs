using Micser.Common.Extensions;
using NLog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 4014

namespace Micser.Common.Api
{
    /// <inheritdoc cref="IApiServer" />
    public class ApiServer : ApiEndPoint, IApiServer
    {
        private readonly TcpListener _listener;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _startSemaphore;

        /// <inheritdoc />
        public ApiServer(IApiConfiguration configuration, IRequestProcessorFactory requestProcessorFactory, ILogger logger)
            : base(configuration, requestProcessorFactory)
        {
            _logger = logger;
            var endPoint = new IPEndPoint(IPAddress.Loopback, Configuration.Port);
            _listener = new TcpListener(endPoint);
            _startSemaphore = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc />
        public bool IsRunning { get; private set; }

        /// <inheritdoc />
        public override async Task ConnectAsync()
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                await _startSemaphore.WaitAsync();

                InClient = null;
                OutClient = null;

                InClient = await _listener.AcceptTcpClientAsync();
                InClient.Client.SetKeepAlive();
                InStream = InClient.GetStream();

                OutClient = await _listener.AcceptTcpClientAsync();
                OutClient.Client.SetKeepAlive();
                OutStream = OutClient.GetStream();

                IsRunning = true;

                Task.Run(ReaderThread);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            finally
            {
                try
                {
                    _startSemaphore?.Release();
                }
                catch
                {
                    // ignored
                }
            }
        }

        /// <inheritdoc />
        /// <exception cref="ObjectDisposedException" />
        public void Start()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(ApiServer));
            }

            _listener.Start();
            ConnectTask = ConnectAsync();
        }

        /// <inheritdoc />
        public void Stop()
        {
            IsRunning = false;
            InClient?.Close();
            OutClient?.Close();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listener?.Stop();
                _startSemaphore?.Dispose();
                Stop();
            }

            base.Dispose(disposing);
        }
    }
}