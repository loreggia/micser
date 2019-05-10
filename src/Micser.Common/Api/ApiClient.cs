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
        public ApiClient(IRequestProcessorFactory requestProcessorFactory, ILogger logger)
            : base(requestProcessorFactory)
        {
            _logger = logger;
            _connectSemaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Tries to connect to an <see cref="ApiServer"/>. Waits until a connection is established.
        /// </summary>
        public override async Task ConnectAsync()
        {
            try
            {
                await _connectSemaphore.WaitAsync();

                InClient?.Dispose();
                OutClient?.Dispose();

                OutClient = new TcpClient();
                await OutClient.ConnectAsync(IPAddress.Loopback, Globals.ApiPort);
                OutClient.Client.SetKeepAlive();
                var outStream = OutClient.GetStream();
                OutReader = new StreamReader(outStream);
                OutWriter = new StreamWriter(outStream) { AutoFlush = true };

                InClient = new TcpClient();
                await InClient.ConnectAsync(IPAddress.Loopback, Globals.ApiPort);
                InClient.Client.SetKeepAlive();
                var inStream = InClient.GetStream();
                InReader = new StreamReader(inStream);
                InWriter = new StreamWriter(inStream) { AutoFlush = true };

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