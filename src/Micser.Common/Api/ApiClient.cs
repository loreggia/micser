using Micser.Common.Extensions;
using NLog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

#pragma warning disable 4014

namespace Micser.Common.Api
{
    /// <summary>
    /// An API endpoint that acts as the client upon connection. Communication is otherwise bidirectional.
    /// </summary>
    public class ApiClient : ApiEndPoint
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ApiClient"/> class.
        /// </summary>
        public ApiClient(IApiConfiguration configuration, IRequestProcessorFactory requestProcessorFactory, ILogger logger)
            : base(configuration, requestProcessorFactory, logger)
        {
        }

        /// <summary>
        /// Tries to connect to an <see cref="ApiServer"/>. Waits until a connection is established.
        /// </summary>
        public override async Task<bool> ConnectAsync()
        {
            if (IsDisposed || _state != EndPointState.Disconnected)
            {
                return false;
            }

            try
            {
                Logger.Info("API client connecting");

                lock (StateLock)
                {
                    if (_state != EndPointState.Disconnected)
                    {
                        return false;
                    }

                    _state = EndPointState.Connecting;
                }

                OutClient = new TcpClient();
                await OutClient.ConnectAsync(IPAddress.Loopback, Configuration.Port);
                if (OutClient.Client == null)
                {
                    OutClient?.Dispose();
                    OutClient = null;
                    return false;
                }

                OutClient.Client.SetKeepAlive();
                OutStream = OutClient.GetStream();

                InClient = new TcpClient();
                await InClient.ConnectAsync(IPAddress.Loopback, Configuration.Port);
                if (InClient.Client == null)
                {
                    OutClient?.Dispose();
                    OutClient = null;
                    InClient?.Dispose();
                    InClient = null;
                    return false;
                }

                InClient.Client.SetKeepAlive();
                InStream = InClient.GetStream();

                lock (StateLock)
                {
                    _state = EndPointState.Connected;
                }

                Logger.Info("API client connected");

                Task.Run(ReaderThread);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                lock (StateLock)
                {
                    OutClient?.Dispose();
                    OutClient = null;
                    InClient?.Dispose();
                    InClient = null;

                    _state = EndPointState.Disconnected;
                }

                return false;
            }
        }
    }
}