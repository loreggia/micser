using Micser.Common.Extensions;
using NLog;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public class ApiClient : DisposableBase, IApiClient
    {
        private const int ConnectTimeout = 10;
        private readonly IApiClientConfiguration _configuration;
        private readonly SemaphoreSlim _connectSemaphore = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;
        private readonly IMessageSerializer _messageSerializer;
        private readonly MemoryStream _readerStream;
        private readonly SemaphoreQueue _sendMessageSemaphore = new SemaphoreQueue(1);
        private readonly object _stateLock = new object();
        private NamedPipeClientStream _pipe;
        private ConnectionState _state;

        public ApiClient(IApiClientConfiguration configuration, IMessageSerializer messageSerializer, ILogger logger)
        {
            _configuration = configuration;
            _messageSerializer = messageSerializer;
            _logger = logger;

            _readerStream = new MemoryStream(1024);
        }

        public ConnectionState State
        {
            get
            {
                if (_state == ConnectionState.Connected && _pipe != null && !_pipe.IsConnected)
                {
                    lock (_stateLock)
                    {
                        _state = ConnectionState.Disconnected;
                    }
                }

                return _state;
            }
            private set => _state = value;
        }

        public async Task<bool> ConnectAsync(CancellationToken token)
        {
            if (State != ConnectionState.Disconnected)
            {
                return false;
            }

            await _connectSemaphore.WaitAsync(token).ConfigureAwait(false);

            lock (_stateLock)
            {
                if (State != ConnectionState.Disconnected)
                {
                    return false;
                }
            }

            try
            {
                if (_pipe != null)
                {
                    await _pipe.DisposeAsync().ConfigureAwait(false);
                }

                _pipe = new NamedPipeClientStream(".", _configuration.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);

                await _pipe.ConnectAsync(ConnectTimeout, token).ConfigureAwait(false);
                _pipe.ReadMode = PipeTransmissionMode.Message;

                lock (_stateLock)
                {
                    State = ConnectionState.Connected;
                }

                return true;
            }
            catch
            {
                lock (_stateLock)
                {
                    State = ConnectionState.Disconnected;
                }

                return false;
            }
            finally
            {
                _connectSemaphore.Release();
            }
        }

        public void Disconnect()
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            lock (_stateLock)
            {
                if (State != ConnectionState.Connected)
                {
                    return;
                }

                State = ConnectionState.Disconnecting;
            }

            _pipe?.Dispose();

            lock (_stateLock)
            {
                State = ConnectionState.Disconnected;
            }
        }

        public async Task<ApiResponse> SendMessageAsync(ApiRequest request, CancellationToken token)
        {
            if (State != ConnectionState.Connected && !await ConnectAsync(token).ConfigureAwait(false))
            {
                return new ApiResponse(false);
            }

            await _sendMessageSemaphore.WaitAsync().ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            try
            {
                await _messageSerializer.SerializeAsync(_pipe, request).ConfigureAwait(false);
                await Task.Run(() => _pipe.WaitForPipeDrain(), token).ConfigureAwait(false);

                _readerStream.Position = 0;
                var count = await _pipe.CopyMessageToAsync(_readerStream, token).ConfigureAwait(false);
                if (count == 0)
                {
                    return new ApiResponse(false);
                }

                _readerStream.Position = 0;
                return await _messageSerializer.DeserializeAsync<ApiResponse>(_readerStream).ConfigureAwait(false);
            }
            catch (IOException)
            {
                Disconnect();
                return new ApiResponse(false);
            }
            finally
            {
                _sendMessageSemaphore.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                Disconnect();
                _readerStream.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }
}