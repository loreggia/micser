using Micser.Common.Extensions;
using NLog;
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
        private readonly IMessageSerializer _messageSerializer;
        private readonly SemaphoreSlim _readerSemaphore = new SemaphoreSlim(1, 1);
        private readonly MemoryStream _readerStream;
        private readonly IRequestProcessorFactory _requestProcessorFactory;
        private readonly object _stateLock = new object();
        private CancellationTokenSource _cancellationTokenSource;
        private NamedPipeServerStream _pipe;

        public ApiServer(IApiServerConfiguration configuration, IRequestProcessorFactory requestProcessorFactory, IMessageSerializer messageSerializer, ILogger logger)
        {
            _configuration = configuration;
            _requestProcessorFactory = requestProcessorFactory;
            _messageSerializer = messageSerializer;
            _logger = logger;

            _readerStream = new MemoryStream(1024);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public ServerState State { get; private set; }

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
                    PipeTransmissionMode.Message, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
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

                _readerSemaphore.Wait(100);
                _pipe?.Dispose();
                _pipe = null;
            }
            finally
            {
                _readerSemaphore.Release();
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
                _readerStream.Dispose();
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
                if (_pipe == null)
                {
                    return;
                }

                _pipe.EndWaitForConnection(ar);

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                var token = _cancellationTokenSource.Token;

                Task.Run(() => ReaderThread(token), token);
            }
            catch
            {
                Stop();
            }
        }

        private async Task ReaderThread(CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await _readerSemaphore.WaitAsync(ct);
                    ct.ThrowIfCancellationRequested();

                    _readerStream.Position = 0;
                    var count = await _pipe.CopyMessageToAsync(_readerStream, ct);

                    if (count == 0)
                    {
                        _readerSemaphore.Release();
                        Stop();
                        await StartAsync().ConfigureAwait(false);
                        return;
                    }

                    _readerStream.Position = 0;
                    var request = await _messageSerializer.DeserializeAsync<ApiRequest>(_readerStream).ConfigureAwait(false);

                    if (request == null)
                    {
                        continue;
                    }

                    var requestProcessor = _requestProcessorFactory.Create(request.Resource);
                    var response = await requestProcessor.ProcessAsync(request.Action, request.Content).ConfigureAwait(false);

                    await Task.Run(() => _pipe.WaitForPipeDrain()).ConfigureAwait(false);

                    await _messageSerializer.SerializeAsync(_pipe, response).ConfigureAwait(false);
                    _readerSemaphore.Release();
                }
                catch (OperationCanceledException)
                {
                    _readerSemaphore.Release();
                    return;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    _readerSemaphore.Release();
                    Stop();
                    await StartAsync().ConfigureAwait(false);
                }
            }
        }
    }
}