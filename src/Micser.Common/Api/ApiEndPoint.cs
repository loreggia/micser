using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public abstract class ApiEndPoint : IDisposable
    {
        protected Task ConnectTask;
        protected TcpClient InClient;
        protected StreamReader InReader;
        protected StreamWriter InWriter;
        protected TcpClient OutClient;
        protected StreamReader OutReader;
        protected StreamWriter OutWriter;
        private readonly IRequestProcessorFactory _requestProcessorFactory;
        private readonly SemaphoreQueue _sendMessageSemaphore;

        protected ApiEndPoint(IRequestProcessorFactory requestProcessorFactory)
        {
            _requestProcessorFactory = requestProcessorFactory;
            _sendMessageSemaphore = new SemaphoreQueue(1);
        }

        public abstract Task ConnectAsync();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
                await OutWriter.WriteLineAsync(json);
                json = await OutReader.ReadLineAsync();
                return JsonConvert.DeserializeObject<JsonResponse>(json);
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
            finally
            {
                _sendMessageSemaphore.Release();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                InReader?.Dispose();
                InWriter?.Dispose();
                InClient?.Dispose();
                OutReader?.Dispose();
                OutWriter?.Dispose();
                OutClient?.Dispose();
            }
        }

        protected async void ReaderThread()
        {
            while (InClient?.Connected == true)
            {
                try
                {
                    var message = await InReader.ReadLineAsync();
                    if (message != null)
                    {
                        var response = ProcessMessage(message);
                        await InWriter.WriteLineAsync(response);
                    }
                }
                catch
                {
                    ConnectTask = ConnectAsync();
                    await ConnectTask;
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