using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public abstract class ApiEndPoint : IDisposable
    {
        protected Task _connectTask;
        protected TcpClient _inClient;
        protected StreamReader _inReader;
        protected StreamWriter _inWriter;
        protected TcpClient _outClient;
        protected StreamReader _outReader;
        protected StreamWriter _outWriter;
        private readonly IRequestProcessorFactory _requestProcessorFactory;
        private readonly SemaphoreSlim _sendMessageSemaphore;

        protected ApiEndPoint(IRequestProcessorFactory requestProcessorFactory)
        {
            _requestProcessorFactory = requestProcessorFactory;
            _sendMessageSemaphore = new SemaphoreSlim(1, 1);
        }

        public abstract Task ConnectAsync();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<JsonResponse> SendMessageAsync(JsonRequest message, int numRetries = 5)
        {
            if (_outClient == null || !_outClient.Connected)
            {
                await ConnectAsync();
            }

            await _sendMessageSemaphore.WaitAsync();

            try
            {
                var json = JsonConvert.SerializeObject(message);
                await _outWriter.WriteLineAsync(json);
                json = await _outReader.ReadLineAsync();
                return JsonConvert.DeserializeObject<JsonResponse>(json);
            }
            catch
            {
                if (numRetries > 0)
                {
                    await Task.Delay(10);
                    return await SendMessageAsync(message, --numRetries);
                }

                throw;
            }
            finally
            {
                _sendMessageSemaphore.Release();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inReader?.Dispose();
                _inWriter?.Dispose();
                _inClient?.Dispose();
                _outReader?.Dispose();
                _outWriter?.Dispose();
                _outClient?.Dispose();
                _sendMessageSemaphore?.Dispose();
            }
        }

        protected async void ReaderThread()
        {
            while (_inClient?.Connected == true)
            {
                try
                {
                    var message = await _inReader.ReadLineAsync();
                    if (message != null)
                    {
                        var response = ProcessMessage(message);
                        await _inWriter.WriteLineAsync(response);
                    }
                }
                catch
                {
                    _connectTask = ConnectAsync();
                    await _connectTask;
                    return;
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
    }
}