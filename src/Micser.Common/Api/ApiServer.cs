using Micser.Common.Extensions;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

#pragma warning disable 4014

namespace Micser.Common.Api
{
    public class ApiServer : IApiServer
    {
        private readonly TcpListener _listener;

        private TcpClient _inClient;
        private StreamReader _inReader;
        private StreamWriter _inWriter;
        private bool _isStarting;
        private TcpClient _outClient;
        private StreamReader _outReader;
        private StreamWriter _outWriter;

        public ApiServer()
        {
            var endPoint = new IPEndPoint(IPAddress.Loopback, Globals.ApiPort);
            _listener = new TcpListener(endPoint);
        }

        public bool IsRunning { get; private set; }

        public Task StartupTask { get; private set; }

        public void Dispose()
        {
            _listener.Stop();
            Stop();
        }

        public async Task<JsonResponse> SendMessageAsync(JsonRequest message, int numRetries = 5)
        {
            if (_outClient == null || !_outClient.Connected)
            {
                await StartAcceptConnectionsAsync();
            }

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
        }

        public void Start()
        {
            _listener.Start();
            StartupTask = StartAcceptConnectionsAsync();
        }

        public void Stop()
        {
            IsRunning = false;
            _inClient?.Close();
            _outClient?.Close();
        }

        private string ProcessMessage(string content)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<JsonRequest>(content);
                return JsonConvert.SerializeObject(new JsonResponse(true, null, null));
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new JsonResponse(false, ex.ToString(), ex.Message));
            }
        }

        private async void ReaderThread()
        {
            while (IsRunning)
            {
                try
                {
                    var message = await _inReader.ReadLineAsync();
                    var response = ProcessMessage(message);
                    await _inWriter.WriteLineAsync(response);
                }
                catch
                {
                    StartupTask = StartAcceptConnectionsAsync();
                    await StartupTask;
                    return;
                }
            }
        }

        private async Task StartAcceptConnectionsAsync()
        {
            if (_isStarting)
            {
                return;
            }

            try
            {
                _isStarting = true;

                _inClient = null;
                _outClient = null;

                _inClient = await _listener.AcceptTcpClientAsync();
                _inClient.Client.SetKeepAlive();
                var inStream = _inClient.GetStream();
                _inReader = new StreamReader(inStream);
                _inWriter = new StreamWriter(inStream) { AutoFlush = true };

                _outClient = await _listener.AcceptTcpClientAsync();
                _outClient.Client.SetKeepAlive();
                var outStream = _outClient.GetStream();
                _outReader = new StreamReader(outStream);
                _outWriter = new StreamWriter(outStream) { AutoFlush = true };

                IsRunning = true;

                Task.Run(() => ReaderThread());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                _isStarting = false;
            }
        }
    }
}