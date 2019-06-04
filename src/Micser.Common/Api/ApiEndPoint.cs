using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    /// <summary>
    /// Base API endpoint implementation. Contains functionality shared between <see cref="ApiServer"/> and <see cref="ApiClient"/>.
    /// </summary>
    /// <inheritdoc cref="IApiEndPoint"/>
    public abstract class ApiEndPoint : IApiEndPoint, IDisposable
    {
        protected static readonly object StateLock = new object();

        protected readonly IApiConfiguration Configuration;

        protected readonly ILogger Logger;

        protected EndPointState _state;

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

            _state = EndPointState.Disconnected;
            _sendMessageSemaphore = new SemaphoreQueue(1);
        }

        public EndPointState State => _state;

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
        public async Task<JsonResponse> SendMessageAsync(JsonRequest message)
        {
            if (_state != EndPointState.Connected)
            {
                return new JsonResponse(false, null, null, false);
            }

            await _sendMessageSemaphore.WaitAsync();

            try
            {
                var json = JsonConvert.SerializeObject(message);

                await ApiProtocol.WriteMessage(OutStream, json);
                var response = await ApiProtocol.ReceiveMessage(OutStream);

                if (response == null)
                {
                    Disconnect();
                    return new JsonResponse { IsSuccess = false };
                }

                return JsonConvert.DeserializeObject<JsonResponse>(response);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new JsonResponse(false, ex, ex.Message);
            }
            finally
            {
                _sendMessageSemaphore.Release();
            }
        }

        protected virtual void Disconnect()
        {
            lock (StateLock)
            {
                _state = EndPointState.Disconnecting;

                InClient?.Dispose();
                InClient = null;
                OutClient?.Dispose();
                OutClient = null;

                _state = EndPointState.Disconnected;
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
                    Disconnect();
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
            while (_state == EndPointState.Connected)
            {
                try
                {
                    var message = await ApiProtocol.ReceiveMessage(InStream);
                    if (message == null)
                    {
                        Disconnect();
                    }
                    var response = ProcessMessage(message);
                    await ApiProtocol.WriteMessage(InStream, response);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    Disconnect();
                }
            }
        }

        private string ProcessMessage(string content)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<JsonRequest>(content);
                if (message == null)
                {
                    return null;
                }
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

                var readCount = await tcpStream.ReadAsync(reqCountBytes, 0, reqCountBytes.Length);

                if (readCount != reqCountBytes.Length)
                {
                    return null;
                }

                var reqCount = BitConverter.ToInt32(reqCountBytes, 0);
                var reqBytes = new byte[reqCount];
                readCount = await tcpStream.ReadAsync(reqBytes, 0, reqCount);

                if (readCount != reqCount)
                {
                    return null;
                }

                return Encoding.UTF8.GetString(reqBytes);
            }

            public static async Task WriteMessage(Stream tcpStream, string content)
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);
                var countBytes = BitConverter.GetBytes(contentBytes.Length);

                await tcpStream.WriteAsync(countBytes, 0, countBytes.Length);
                await tcpStream.FlushAsync();
                await tcpStream.WriteAsync(contentBytes, 0, contentBytes.Length);
                await tcpStream.FlushAsync();
            }
        }
    }
}