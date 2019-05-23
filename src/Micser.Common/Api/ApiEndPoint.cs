using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public enum EndpointState
    {
        Disconnected,
        Disconnecting,
        Connected,
        Connecting
    }

    /// <summary>
    /// Base API endpoint implementation. Contains functionality shared between <see cref="ApiServer"/> and <see cref="ApiClient"/>.
    /// </summary>
    /// <inheritdoc cref="IApiEndPoint"/>
    public abstract class ApiEndPoint : IApiEndPoint, IDisposable
    {
        protected static readonly object StateLock = new object();

        protected readonly IApiConfiguration Configuration;

        protected readonly ILogger Logger;

        protected EndpointState _state;

        /// <summary>
        /// The task that is created by the <see cref="ConnectAsync"/> method.
        /// </summary>
        protected Task ConnectTask;

        /// <summary>
        /// Client that handles incoming messages.
        /// </summary>
        protected TcpClient InClient;

        protected Stream InStream;

        protected bool IsDisposed;

        /// <summary>
        /// Handles outgoing messages.
        /// </summary>
        protected TcpClient OutClient;

        protected Stream OutStream;

        private readonly IRequestProcessorFactory _requestProcessorFactory;
        private readonly SemaphoreQueue _sendMessageSemaphore;

        /// <inheritdoc />
        protected ApiEndPoint(IApiConfiguration configuration, IRequestProcessorFactory requestProcessorFactory, ILogger logger)
        {
            Configuration = configuration;
            _requestProcessorFactory = requestProcessorFactory;
            Logger = logger;

            _state = EndpointState.Disconnected;
            _sendMessageSemaphore = new SemaphoreQueue(1);
        }

        /// <summary>
        /// Tries to connect to the API counterpart.
        /// </summary>
        public abstract Task<bool> ConnectAsync();

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public async Task<JsonResponse> SendMessageAsync(JsonRequest message, int numRetries = 5)
        {
            await _sendMessageSemaphore.WaitAsync();

            if (OutClient == null || !OutClient.Connected)
            {
                await ConnectAsync();
            }

            try
            {
                var json = JsonConvert.SerializeObject(message);

                await ApiProtocol.WriteMessage(OutStream, json);
                var response = await ApiProtocol.ReceiveMessage(OutStream);
                _sendMessageSemaphore.Release();
                return JsonConvert.DeserializeObject<JsonResponse>(response);
            }
            catch
            {
                _sendMessageSemaphore.Release();

                if (numRetries > 0)
                {
                    await Task.Delay(10);
                    return await SendMessageAsync(message, --numRetries);
                }

                throw;
            }
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    lock (StateLock)
                    {
                        InClient?.Dispose();
                        InClient = null;
                        OutClient?.Dispose();
                        OutClient = null;

                        _state = EndpointState.Disconnected;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            IsDisposed = true;
        }

        /// <summary>
        /// Thread loop that awaits and reads incoming messages.
        /// </summary>
        protected async void ReaderThread()
        {
            while (_state == EndpointState.Connected)
            {
                try
                {
                    var message = await ApiProtocol.ReceiveMessage(InStream);
                    var response = ProcessMessage(message);
                    await ApiProtocol.WriteMessage(InStream, response);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        private string ProcessMessage(string content)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<JsonRequest>(content);
                var processor = _requestProcessorFactory.Create(message.Resource);
                var response = processor.Process(message.Action, message.Content);
                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new JsonResponse(false, ex.ToString(), ex.Message));
            }
        }

        private class ApiProtocol
        {
            public static async Task<string> ReceiveMessage(Stream tcpStream)
            {
                var reqCountBytes = new byte[sizeof(int)];
                await tcpStream.ReadAsync(reqCountBytes, 0, reqCountBytes.Length);
                var reqCount = BitConverter.ToInt32(reqCountBytes, 0);
                var reqBytes = new byte[reqCount];
                await tcpStream.ReadAsync(reqBytes, 0, reqCount);
                return Encoding.UTF8.GetString(reqBytes);
            }

            public static async Task WriteMessage(Stream tcpStream, string content)
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);
                var countBytes = BitConverter.GetBytes(contentBytes.Length);
                await tcpStream.WriteAsync(countBytes, 0, countBytes.Length);
                await tcpStream.WriteAsync(contentBytes, 0, contentBytes.Length);
            }
        }
    }
}