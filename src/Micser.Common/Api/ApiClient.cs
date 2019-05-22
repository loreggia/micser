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
    public enum ClientState
    {
        Disconnected,
        Disconnecting,
        Connected,
        Connecting
    }

    /// <summary>
    /// An API endpoint that acts as the client upon connection. Communication is otherwise bidirectional.
    /// </summary>
    public class ApiClient : ApiEndPoint
    {
        private readonly SemaphoreSlim _connectSemaphore;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of the <see cref="ApiClient"/> class.
        /// </summary>
        public ApiClient(IApiConfiguration configuration, IRequestProcessorFactory requestProcessorFactory, ILogger logger)
            : base(configuration, requestProcessorFactory)
        {
            _logger = logger;
            _connectSemaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Tries to connect to an <see cref="ApiServer"/>. Waits until a connection is established.
        /// </summary>
        public override async Task ConnectAsync()
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                await _connectSemaphore.WaitAsync();

                InClient?.Dispose();
                OutClient?.Dispose();

                OutClient = new TcpClient();
                await OutClient.ConnectAsync(IPAddress.Loopback, Configuration.Port);
                if (OutClient.Client == null)
                {
                    return;
                }

                OutClient.Client.SetKeepAlive();
                OutStream = OutClient.GetStream();

                InClient = new TcpClient();
                await InClient.ConnectAsync(IPAddress.Loopback, Configuration.Port);
                if (InClient.Client == null)
                {
                    return;
                }

                InClient.Client.SetKeepAlive();
                InStream = InClient.GetStream();

                Task.Run(ReaderThread);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            finally
            {
                _connectSemaphore.Release();
            }
        }
    }
}