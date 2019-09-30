using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    /// <summary>
    /// Base API endpoint implementation. Contains functionality shared between <see cref="ApiServer"/> and <see cref="ApiClient"/>.
    /// </summary>
    /// <inheritdoc cref="IApiEndPoint"/>
    public abstract class ApiEndPoint : IApiEndPoint, IDisposable
    {
        /// <summary>
        /// The lock object to use when accessing the <see cref="State"/> field.
        /// </summary>
        protected static readonly object StateLock = new object();

        /// <summary>
        /// The currently used API configuration.
        /// </summary>
        protected readonly IApiConfiguration Configuration;

        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// The task that is created by the <see cref="ConnectAsync"/> method.
        /// </summary>
        protected Task ConnectTask;

        /// <summary>
        /// Client that handles incoming messages.
        /// </summary>
        protected TcpClient InClient;

        /// <summary>
        /// The TCP stream for receiving data.
        /// </summary>
        protected Stream InStream;

        /// <summary>
        /// Indicates whether the instance has been disposed of.
        /// </summary>
        protected bool IsDisposed;

        /// <summary>
        /// Handles outgoing messages.
        /// </summary>
        protected TcpClient OutClient;

        /// <summary>
        /// The TCP stream for sending data.
        /// </summary>
        protected Stream OutStream;

        private readonly IRequestProcessorFactory _requestProcessorFactory;
        private readonly SemaphoreQueue _sendMessageSemaphore;
        private bool _isReading;

        /// <inheritdoc />
        protected ApiEndPoint(IApiConfiguration configuration, IRequestProcessorFactory requestProcessorFactory, ILogger logger)
        {
            Configuration = configuration;
            _requestProcessorFactory = requestProcessorFactory;
            Logger = logger;

            State = EndPointState.Disconnected;
            _sendMessageSemaphore = new SemaphoreQueue(1);
        }

        /// <summary>
        /// Gets the current <see cref="EndPointState"/>.
        /// </summary>
        public EndPointState State { get; protected set; }

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
            if (State != EndPointState.Connected)
            {
                return new JsonResponse(false, null, null, false);
            }

            if (_sendMessageSemaphore.WaiterCount > 5)
            {
                Logger.Warn($"{nameof(SendMessageAsync)} waiters: {_sendMessageSemaphore.WaiterCount}");
            }

            await _sendMessageSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var json = JsonConvert.SerializeObject(message);

                await ApiProtocol.WriteMessage(OutStream, json).ConfigureAwait(false);
                var response = await ApiProtocol.ReceiveMessage(OutStream).ConfigureAwait(false);

                if (response == null)
                {
                    Disconnect();
                    return new JsonResponse { IsSuccess = false, IsConnected = false };
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

        /// <summary>
        /// Disposes the current streams and TCP clients and sets the <see cref="State"/> to <see cref="EndPointState.Disconnected"/>.
        /// </summary>
        protected virtual void Disconnect()
        {
            if (State == EndPointState.Disconnected ||
                State == EndPointState.Disconnecting)
            {
                return;
            }

            Logger.Info("Disconnecting");

            lock (StateLock)
            {
                State = EndPointState.Disconnecting;

                InStream?.Dispose();
                InStream = null;
                InClient?.Dispose();
                InClient = null;
                OutStream?.Dispose();
                OutStream = null;
                OutClient?.Dispose();
                OutClient = null;

                State = EndPointState.Disconnected;
            }

            while (_isReading)
            {
                Thread.Sleep(10);
            }

            Logger.Info("Disconnected");
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
            while (State == EndPointState.Connected)
            {
                try
                {
                    _isReading = true;
                    var message = await ApiProtocol.ReceiveMessage(InStream).ConfigureAwait(false);
                    if (message == null && State == EndPointState.Connected)
                    {
                        Disconnect();
                        return;
                    }

                    var response = await ProcessMessage(message).ConfigureAwait(false);
                    await ApiProtocol.WriteMessage(InStream, response).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (!(ex is IOException ioEx) || ioEx.HResult != -2146232800)
                    {
                        Logger.Error(ex);
                    }

                    Disconnect();
                }
                finally
                {
                    _isReading = false;
                }
            }
        }

        private async Task<string> ProcessMessage(string content)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<JsonRequest>(content);
                if (message == null)
                {
                    return null;
                }
                var processor = _requestProcessorFactory.Create(message.Resource);
                var response = await processor.ProcessAsync(message.Action, message.Content).ConfigureAwait(false);
                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new JsonResponse(false, ex.ToString(), ex.Message));
            }
        }

        private static class ApiProtocol
        {
            public static async Task<string> ReceiveMessage(Stream tcpStream)
            {
                if (tcpStream == null)
                {
                    return null;
                }

                var headerBuffer = new byte[sizeof(int)];
                var headerRead = 0;
                int bytesRead;

                do
                {
                    bytesRead = await tcpStream.ReadAsync(headerBuffer, headerRead, headerBuffer.Length - headerRead).ConfigureAwait(false);
                    headerRead += bytesRead;
                }
                while (headerRead < headerBuffer.Length && bytesRead > 0);

                if (headerRead != headerBuffer.Length)
                {
                    return null;
                }

                var contentLength = BitConverter.ToInt32(headerBuffer, 0);
                var contentBuffer = new byte[contentLength];
                var contentRead = 0;

                do
                {
                    bytesRead = await tcpStream.ReadAsync(contentBuffer, contentRead, contentLength - contentRead).ConfigureAwait(false);
                    contentRead += bytesRead;
                }
                while (contentRead < contentLength && bytesRead > 0);

                if (contentRead != contentLength)
                {
                    return null;
                }

                return Encoding.UTF8.GetString(contentBuffer);
            }

            public static async Task WriteMessage(Stream tcpStream, string content)
            {
                if (tcpStream == null)
                {
                    return;
                }

                var contentBytes = Encoding.UTF8.GetBytes(content);
                var headerBytes = BitConverter.GetBytes(contentBytes.Length);

                await tcpStream.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);
                await tcpStream.FlushAsync().ConfigureAwait(false);
                await tcpStream.WriteAsync(contentBytes, 0, contentBytes.Length).ConfigureAwait(false);
                await tcpStream.FlushAsync().ConfigureAwait(false);
            }
        }
    }
}