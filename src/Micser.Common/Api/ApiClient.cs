using Micser.Common.Extensions;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 4014

namespace Micser.Common.Api
{
    public class ApiClient : IApiClient
    {
        private readonly IRequestProcessorFactory _requestProcessorFactory;
        private readonly SemaphoreSlim _sendMessageSemaphore;
        private TcpClient _inClient;
        private StreamReader _inReader;
        private StreamWriter _inWriter;
        private bool _isConnecting;
        private TcpClient _outClient;
        private StreamReader _outReader;
        private StreamWriter _outWriter;

        public ApiClient(IRequestProcessorFactory requestProcessorFactory)
        {
            _requestProcessorFactory = requestProcessorFactory;
            _sendMessageSemaphore = new SemaphoreSlim(1, 1);
        }

        public async Task ConnectAsync()
        {
            if (_isConnecting)
            {
                return;
            }

            try
            {
                _isConnecting = true;

                _inClient?.Dispose();
                _outClient?.Dispose();

                _outClient = new TcpClient();
                await _outClient.ConnectAsync(IPAddress.Loopback, Globals.ApiPort);
                _outClient.Client.SetKeepAlive();
                var outStream = _outClient.GetStream();
                _outReader = new StreamReader(outStream);
                _outWriter = new StreamWriter(outStream) { AutoFlush = true };

                _inClient = new TcpClient();
                await _inClient.ConnectAsync(IPAddress.Loopback, Globals.ApiPort);
                _inClient.Client.SetKeepAlive();
                var inStream = _inClient.GetStream();
                _inReader = new StreamReader(inStream);
                _inWriter = new StreamWriter(inStream) { AutoFlush = true };

                Task.Run(() => ReaderThread());
            }
            catch (Exception ex)
            {
                //todo
                Console.WriteLine(ex);
            }
            finally
            {
                _isConnecting = false;
            }
        }

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
                _inClient?.Dispose();
                _outClient?.Dispose();
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

        private async void ReaderThread()
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
                    await ConnectAsync();
                    return;
                }
            }
        }
    }
}