using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
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
        /// The task that is created by the <see cref="ConnectAsync"/> method.
        /// </summary>
        protected Task ConnectTask;

        /// <summary>
        /// Client that handles incoming messages.
        /// </summary>
        protected TcpClient InClient;

        /// <summary>
        /// Reader that reads incoming messages.
        /// </summary>
        protected StreamReader InReader;

        /// <summary>
        /// Writer that responds to incoming messages.
        /// </summary>
        protected StreamWriter InWriter;

        /// <summary>
        /// Handles outgoing messages.
        /// </summary>
        protected TcpClient OutClient;

        /// <summary>
        /// Handles the response after sending a message.
        /// </summary>
        protected StreamReader OutReader;

        /// <summary>
        /// Writes outgoing messages.
        /// </summary>
        protected StreamWriter OutWriter;

        private readonly IRequestProcessorFactory _requestProcessorFactory;
        private readonly SemaphoreQueue _sendMessageSemaphore;

        /// <summary>
        /// Creates an instance of the <see cref="ApiEndPoint"/> class.
        /// </summary>
        protected ApiEndPoint(IRequestProcessorFactory requestProcessorFactory)
        {
            _requestProcessorFactory = requestProcessorFactory;
            _sendMessageSemaphore = new SemaphoreQueue(1);
        }

        /// <summary>
        /// Tries to connect to the API counterpart.
        /// </summary>
        public abstract Task ConnectAsync();

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
                await OutWriter.WriteLineAsync(json);
                json = await OutReader.ReadLineAsync();
                _sendMessageSemaphore.Release();
                return JsonConvert.DeserializeObject<JsonResponse>(json);
            }
            catch
            {
                if (numRetries > 0)
                {
                    _sendMessageSemaphore.Release();
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
                    InReader?.Dispose();
                    InWriter?.Dispose();
                    InClient?.Dispose();
                    OutReader?.Dispose();
                    OutWriter?.Dispose();
                    OutClient?.Dispose();
                }
                catch
                {
                    // ignored
                }
            }
        }

        /// <summary>
        /// Thread loop that awaits and reads incoming messages.
        /// </summary>
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