using NLog;
using ProtoBuf;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public class ApiClient : DisposableBase, IApiClient
    {
        private const int ConnectTimeout = 1000;
        private readonly IApiClientConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly object _stateLock = new object();
        private NamedPipeClientStream _pipe;
        private SemaphoreQueue _sendMessageSemaphore = new SemaphoreQueue(1);
        private ConnectionState _state;

        public ApiClient(IApiClientConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
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

        public async Task<bool> ConnectAsync()
        {
            if (State != ConnectionState.Disconnected)
            {
                return false;
            }

            lock (_stateLock)
            {
                if (State != ConnectionState.Disconnected)
                {
                    return false;
                }

                State = ConnectionState.Connecting;
            }

            if (_pipe != null)
            {
                await _pipe.DisposeAsync().ConfigureAwait(false);
            }

            _pipe = new NamedPipeClientStream(".", _configuration.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                await _pipe.ConnectAsync(ConnectTimeout);

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

        public async Task<ApiResponse> SendMessageAsync(ApiRequest request)
        {
            await _sendMessageSemaphore.WaitAsync();

            if (State != ConnectionState.Connected && !(await ConnectAsync().ConfigureAwait(false)))
            {
                _sendMessageSemaphore.Release();
                return new ApiResponse(false);
            }

            try
            {
                var ms = new MemoryStream();
                Serializer.SerializeWithLengthPrefix(ms, request, PrefixStyle.Base128);
                await _pipe.WriteAsync(ms.GetBuffer(), 0, (int)ms.Length).ConfigureAwait(false);

                // "dummy" read (0 bytes) so the thread is not blocked and can be cancelled with the cancellation token
                var emptyByteArray = new byte[0];
                await _pipe.ReadAsync(emptyByteArray, 0, 0).ConfigureAwait(false);
                return Serializer.DeserializeWithLengthPrefix<ApiResponse>(_pipe, PrefixStyle.Base128);
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
            }
            catch
            {
                // ignore
            }
        }
    }
}