using NLog;
using ProtoBuf;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public class ApiServer : DisposableBase, IApiServer
    {
        private readonly IApiServerConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IRequestProcessorFactory _requestProcessorFactory;
        private readonly object _stateLock = new object();
        private CancellationTokenSource _cancellationTokenSource;
        private NamedPipeServerStream _pipe;

        public ApiServer(IApiServerConfiguration configuration, IRequestProcessorFactory requestProcessorFactory, ILogger logger)
        {
            _configuration = configuration;
            _requestProcessorFactory = requestProcessorFactory;
            _logger = logger;
        }

        public ConnectionState ConnectionState { get; protected set; }

        public ServerState State { get; protected set; }

        public async Task<bool> StartAsync()
        {
            if (State != ServerState.Stopped)
            {
                return false;
            }

            lock (_stateLock)
            {
                if (State != ServerState.Stopped)
                {
                    return false;
                }

                State = ServerState.Starting;
            }

            if (_pipe != null)
            {
                await _pipe.DisposeAsync().ConfigureAwait(false);
            }

            try
            {
                _pipe = new NamedPipeServerStream(_configuration.PipeName, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                _pipe.BeginWaitForConnection(OnConnectionReceived, null);

                lock (_stateLock)
                {
                    State = ServerState.Started;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                lock (_stateLock)
                {
                    State = ServerState.Stopped;
                }
                return false;
            }
        }

        public void Stop()
        {
            if (State != ServerState.Started)
            {
                return;
            }

            lock (_stateLock)
            {
                if (State != ServerState.Started)
                {
                    return;
                }

                State = ServerState.Stopping;
            }

            try
            {
                if (_cancellationTokenSource != null &&
                    !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }

                _pipe?.Dispose();
            }
            finally
            {
                lock (_stateLock)
                {
                    State = ServerState.Stopped;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                Stop();
            }
            catch
            {
                // ignore
            }
        }

        private void OnConnectionReceived(IAsyncResult ar)
        {
            try
            {
                _pipe.EndWaitForConnection(ar);

                _cancellationTokenSource = new CancellationTokenSource();

                Task.Run(ReaderThread);
            }
            catch
            {
                Stop();
            }
        }

        private async Task ReaderThread()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    // "dummy" read (0 bytes) so the thread is not blocked and can be cancelled with the cancellation token
                    var emptyByteArray = new byte[0];
                    await _pipe.ReadAsync(emptyByteArray, 0, 0, _cancellationTokenSource.Token).ConfigureAwait(false);

                    var request = Serializer.DeserializeWithLengthPrefix<ApiRequest>(_pipe, PrefixStyle.Base128);
                    var requestProcessor = _requestProcessorFactory.Create(request.Resource);
                    var response = await requestProcessor.ProcessAsync(request.Action, request.Content).ConfigureAwait(false);

                    // don't block when writing the response either
                    var ms = new MemoryStream();
                    Serializer.SerializeWithLengthPrefix(ms, response, PrefixStyle.Base128);
                    await _pipe.WriteAsync(ms.GetBuffer(), 0, (int)ms.Length, _cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
        }
    }
}