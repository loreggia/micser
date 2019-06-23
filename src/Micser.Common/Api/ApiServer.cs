using Micser.Common.Extensions;
using NLog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

#pragma warning disable 4014

namespace Micser.Common.Api
{
    /// <inheritdoc cref="IApiServer" />
    public class ApiServer : ApiEndPoint, IApiServer
    {
        private readonly TcpListener _listener;
        private ServerState _serverState;

        /// <inheritdoc />
        public ApiServer(IApiConfiguration configuration, IRequestProcessorFactory requestProcessorFactory, ILogger logger)
            : base(configuration, requestProcessorFactory, logger)
        {
            _serverState = ServerState.Stopped;
            var endPoint = new IPEndPoint(IPAddress.Loopback, Configuration.Port);
            _listener = new TcpListener(endPoint);
        }

        public ServerState ServerState => _serverState;

        /// <inheritdoc />
        public override async Task<bool> ConnectAsync()
        {
            if (IsDisposed || _state != EndPointState.Disconnected || _serverState != ServerState.Started)
            {
                return false;
            }

            try
            {
                Logger.Info("API server connecting");

                lock (StateLock)
                {
                    if (_state != EndPointState.Disconnected || _serverState != ServerState.Started)
                    {
                        return false;
                    }

                    _state = EndPointState.Connecting;
                }

                InClient = await _listener.AcceptTcpClientAsync();
                InClient.Client.SetKeepAlive();
                InStream = InClient.GetStream();

                OutClient = await _listener.AcceptTcpClientAsync();
                OutClient.Client.SetKeepAlive();
                OutStream = OutClient.GetStream();

                lock (StateLock)
                {
                    _state = EndPointState.Connected;
                }

                Logger.Info("API server connected");

                Task.Run(ReaderThread);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        /// <inheritdoc />
        /// <exception cref="ObjectDisposedException" />
        public bool Start()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(ApiServer));
            }

            Logger.Info($"Starting API server. Current state: {_serverState}");

            if (_serverState != ServerState.Stopped)
            {
                return _serverState == ServerState.Started;
            }

            lock (StateLock)
            {
                if (_serverState != ServerState.Stopped)
                {
                    return _serverState == ServerState.Started;
                }

                _serverState = ServerState.Starting;
            }

            try
            {
                _listener.Start();

                lock (StateLock)
                {
                    _serverState = ServerState.Started;
                }

                Logger.Info("API server started");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            Logger.Info("Stopping server");

            if (_serverState != ServerState.Started)
            {
                return;
            }

            lock (StateLock)
            {
                if (_serverState != ServerState.Started)
                {
                    return;
                }

                _serverState = ServerState.Stopping;
                _state = EndPointState.Disconnecting;
            }

            try
            {
                _listener?.Stop();
                InClient?.Close();
                InClient = null;
                OutClient?.Close();
                OutClient = null;
            }
            catch
            {
                // Ignored
            }
            finally
            {
                lock (StateLock)
                {
                    _serverState = ServerState.Stopped;
                    _state = EndPointState.Disconnected;
                }

                Logger.Info("Server stopped");
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }

            base.Dispose(disposing);
        }
    }
}