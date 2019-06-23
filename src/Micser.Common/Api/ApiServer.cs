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

        /// <inheritdoc />
        public ApiServer(IApiConfiguration configuration, IRequestProcessorFactory requestProcessorFactory, ILogger logger)
            : base(configuration, requestProcessorFactory, logger)
        {
            ServerState = ServerState.Stopped;
            var endPoint = new IPEndPoint(IPAddress.Loopback, Configuration.Port);
            _listener = new TcpListener(endPoint);
        }

        /// <inheritdoc />
        public ServerState ServerState { get; protected set; }

        /// <inheritdoc />
        public override async Task<bool> ConnectAsync()
        {
            if (IsDisposed || State != EndPointState.Disconnected || ServerState != ServerState.Started)
            {
                return false;
            }

            try
            {
                Logger.Info("API server connecting");

                lock (StateLock)
                {
                    if (State != EndPointState.Disconnected || ServerState != ServerState.Started)
                    {
                        return false;
                    }

                    State = EndPointState.Connecting;
                }

                InClient = await _listener.AcceptTcpClientAsync();
                InClient.Client.SetKeepAlive();
                InStream = InClient.GetStream();

                OutClient = await _listener.AcceptTcpClientAsync();
                OutClient.Client.SetKeepAlive();
                OutStream = OutClient.GetStream();

                lock (StateLock)
                {
                    State = EndPointState.Connected;
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

            Logger.Info($"Starting API server. Current state: {ServerState}");

            if (ServerState != ServerState.Stopped)
            {
                return ServerState == ServerState.Started;
            }

            lock (StateLock)
            {
                if (ServerState != ServerState.Stopped)
                {
                    return ServerState == ServerState.Started;
                }

                ServerState = ServerState.Starting;
            }

            try
            {
                _listener.Start();

                lock (StateLock)
                {
                    ServerState = ServerState.Started;
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

            if (ServerState != ServerState.Started)
            {
                return;
            }

            lock (StateLock)
            {
                if (ServerState != ServerState.Started)
                {
                    return;
                }

                ServerState = ServerState.Stopping;
                State = EndPointState.Disconnecting;
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
                    ServerState = ServerState.Stopped;
                    State = EndPointState.Disconnected;
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