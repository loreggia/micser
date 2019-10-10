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

        public ApiClient(IApiClientConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public ConnectionState ConnectionState { get; private set; }

        public async Task<bool> ConnectAsync()
        {
            if (ConnectionState != ConnectionState.Disconnected)
            {
                return false;
            }

            lock (_stateLock)
            {
                if (ConnectionState != ConnectionState.Disconnected)
                {
                    return false;
                }

                ConnectionState = ConnectionState.Connecting;
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
                    ConnectionState = ConnectionState.Connected;
                }

                return true;
            }
            catch
            {
                lock (_stateLock)
                {
                    ConnectionState = ConnectionState.Disconnected;
                }

                return false;
            }
        }

        public void Disconnect()
        {
            if (ConnectionState != ConnectionState.Connected)
            {
                return;
            }

            lock (_stateLock)
            {
                if (ConnectionState != ConnectionState.Connected)
                {
                    return;
                }

                ConnectionState = ConnectionState.Disconnecting;
            }

            _pipe?.Dispose();

            lock (_stateLock)
            {
                ConnectionState = ConnectionState.Disconnected;
            }
        }

        public async Task<ApiResponse> SendMessageAsync(ApiRequest request)
        {
            if (ConnectionState != ConnectionState.Connected && !(await ConnectAsync().ConfigureAwait(false)))
            {
                return new ApiResponse(false);
            }

            var ms = new MemoryStream();
            Serializer.SerializeWithLengthPrefix(ms, request, PrefixStyle.Base128);
            await _pipe.WriteAsync(ms.GetBuffer(), 0, (int)ms.Length).ConfigureAwait(false);

            // "dummy" read (0 bytes) so the thread is not blocked and can be cancelled with the cancellation token
            var emptyByteArray = new byte[0];
            await _pipe.ReadAsync(emptyByteArray, 0, 0).ConfigureAwait(false);
            return Serializer.DeserializeWithLengthPrefix<ApiResponse>(_pipe, PrefixStyle.Base128);
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